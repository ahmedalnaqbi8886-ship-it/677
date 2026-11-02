'use strict';
// سيرفر تراخيص - يخزن المفاتيح في MongoDB (دائم) بدل ملف محلي (اللي يروح عند
// كل Redeploy بالخطة المجانية). لو ما حطيت MONGODB_URI بعد، يرجع يستخدم ملف
// licenses.json المحلي تلقائيًا (وضع تجربة فقط - غير دائم).
const http = require('http');
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { MongoClient } = require('mongodb');

const PORT = process.env.PORT || 3000;
const ADMIN_KEY = process.env.ADMIN_KEY || ''; // لازم تحطه كمتغير بيئة على الاستضافة - لا تكتبه بالكود
const MONGODB_URI = process.env.MONGODB_URI || ''; // رابط الاتصال من MongoDB Atlas
const MONGO_DB_NAME = process.env.MONGODB_DBNAME || 'eax_license';
const MONGO_COLL_NAME = 'licenses';
const DB_PATH = path.join(__dirname, 'licenses.json');
const ADMIN_HTML_PATH = path.join(__dirname, 'admin.html');

let mongoColl = null; // يبقى null لو ما فيه MONGODB_URI او فشل الاتصال -> يرجع لملف محلي
let DB = [];

async function connectMongo() {
  if (!MONGODB_URI) {
    console.log('WARNING: MONGODB_URI not set - using local file storage (not persistent on free hosting).');
    return;
  }
  try {
    const client = new MongoClient(MONGODB_URI, { serverSelectionTimeoutMS: 8000 });
    await client.connect();
    mongoColl = client.db(MONGO_DB_NAME).collection(MONGO_COLL_NAME);
    console.log('connected to MongoDB (' + MONGO_DB_NAME + '.' + MONGO_COLL_NAME + ')');
  } catch (e) {
    mongoColl = null;
    console.log('MongoDB connect failed, falling back to local file: ' + e.message);
  }
}

async function loadDb() {
  if (mongoColl) {
    try {
      DB = await mongoColl.find({}, { projection: { _id: 0 } }).toArray();
      return;
    } catch (e) {
      console.log('mongo load failed, falling back to local file: ' + e.message);
    }
  }
  try { DB = JSON.parse(fs.readFileSync(DB_PATH, 'utf8')); } catch (e) { DB = []; }
}

async function saveDb() {
  if (mongoColl) {
    try {
      await mongoColl.deleteMany({});
      if (DB.length) await mongoColl.insertMany(DB);
    } catch (e) {
      console.log('mongo save failed: ' + e.message);
    }
  }
  // نسخة احتياطية محلية دايمًا (تساعد وقت تشغيل الخدمة الحالي حتى لو انقطع مونقو مؤقتًا)
  try { fs.writeFileSync(DB_PATH, JSON.stringify(DB, null, 2)); } catch (e) { }
}

function genKey() {
  const part = () => crypto.randomBytes(3).toString('hex').toUpperCase();
  return 'EAX-' + part() + '-' + part() + '-' + part() + '-' + part();
}

function findKey(key) {
  return DB.find(function (r) { return r.key === key; });
}

// يوحّد شكل الـ HWID قبل المقارنة (يشيل الشرطات والمسافات، احرف كبيرة)
function normHwid(h) {
  return (h || '').toUpperCase().replace(/[^A-F0-9]/g, '');
}

function isAdmin(req, url) {
  if (!ADMIN_KEY) return false; // لو ما حطيت ADMIN_KEY، لوحة التحكم تنقفل تلقائيا لحمايتك
  const given = url.searchParams.get('admin') || req.headers['x-admin-key'];
  return given === ADMIN_KEY;
}

function send(res, code, body, type) {
  res.writeHead(code, {
    'Content-Type': type || 'text/plain; charset=utf-8',
    'Access-Control-Allow-Origin': '*',
    'Cache-Control': 'no-store'
  });
  res.end(body);
}

function statusLine(rec, hwid) {
  if (!rec) return 'NOTFOUND||';
  if (rec.hwid && normHwid(rec.hwid) !== normHwid(hwid)) return 'WRONGHWID||';
  if (rec.revoked) return 'REVOKED|' + rec.plan + '|' + (rec.expires || '');
  if (rec.expires && new Date(rec.expires).getTime() <= Date.now()) return 'EXPIRED|' + rec.plan + '|' + rec.expires;
  return 'ACTIVE|' + rec.plan + '|' + (rec.expires || 'NEVER');
}

let ADMIN_HTML = '';
try { ADMIN_HTML = fs.readFileSync(ADMIN_HTML_PATH, 'utf8'); } catch (e) { ADMIN_HTML = '<h1>admin.html missing</h1>'; }

const server = http.createServer(async function (req, res) {
  let url;
  try { url = new URL(req.url, 'http://x'); } catch (e) { send(res, 400, 'bad request'); return; }
  const p = url.pathname;

  try {
    // ---- نقطة التحقق العامة اللي يناديها الـ DLL عند العميل ----
    if (req.method === 'GET' && p === '/validate') {
      const key = (url.searchParams.get('key') || '').trim();
      const hwid = (url.searchParams.get('hwid') || '').trim();
      const rec = findKey(key);
      send(res, 200, statusLine(rec, hwid));
      return;
    }

    // ---- لوحة التحكم (صفحة ويب) ----
    if (req.method === 'GET' && p === '/admin') {
      send(res, 200, ADMIN_HTML, 'text/html; charset=utf-8');
      return;
    }

    if (req.method === 'GET' && p === '/admin/list') {
      if (!isAdmin(req, url)) { send(res, 401, 'unauthorized'); return; }
      send(res, 200, JSON.stringify(DB), 'application/json');
      return;
    }

    if (req.method === 'POST' && p === '/admin/issue') {
      if (!isAdmin(req, url)) { send(res, 401, 'unauthorized'); return; }
      const plan = url.searchParams.get('plan') || 'Monthly';
      const days = parseInt(url.searchParams.get('days') || '0', 10);
      const note = url.searchParams.get('note') || '';
      const hwid = normHwid(url.searchParams.get('hwid') || '');
      if (!hwid) { send(res, 400, 'hwid is required'); return; }
      let expires = null;
      if (plan !== 'Lifetime' && days > 0) {
        expires = new Date(Date.now() + days * 86400000).toISOString();
      }
      const rec = { key: genKey(), hwid: hwid, plan: plan, issued: new Date().toISOString(), expires: expires, revoked: false, note: note };
      DB.push(rec);
      await saveDb();
      send(res, 200, JSON.stringify(rec), 'application/json');
      return;
    }

    if (req.method === 'POST' && p === '/admin/rebind') {
      if (!isAdmin(req, url)) { send(res, 401, 'unauthorized'); return; }
      const rec = findKey(url.searchParams.get('key') || '');
      if (!rec) { send(res, 404, 'not found'); return; }
      const hwid = normHwid(url.searchParams.get('hwid') || '');
      if (!hwid) { send(res, 400, 'hwid is required'); return; }
      rec.hwid = hwid;
      await saveDb();
      send(res, 200, 'ok');
      return;
    }

    if (req.method === 'POST' && p === '/admin/revoke') {
      if (!isAdmin(req, url)) { send(res, 401, 'unauthorized'); return; }
      const rec = findKey(url.searchParams.get('key') || '');
      if (!rec) { send(res, 404, 'not found'); return; }
      rec.revoked = true;
      await saveDb();
      send(res, 200, 'ok');
      return;
    }

    if (req.method === 'POST' && p === '/admin/reactivate') {
      if (!isAdmin(req, url)) { send(res, 401, 'unauthorized'); return; }
      const rec = findKey(url.searchParams.get('key') || '');
      if (!rec) { send(res, 404, 'not found'); return; }
      rec.revoked = false;
      await saveDb();
      send(res, 200, 'ok');
      return;
    }

    if (req.method === 'POST' && p === '/admin/extend') {
      if (!isAdmin(req, url)) { send(res, 401, 'unauthorized'); return; }
      const rec = findKey(url.searchParams.get('key') || '');
      if (!rec) { send(res, 404, 'not found'); return; }
      const days = parseInt(url.searchParams.get('days') || '0', 10);
      const base = (rec.expires && new Date(rec.expires).getTime() > Date.now()) ? new Date(rec.expires).getTime() : Date.now();
      rec.expires = new Date(base + days * 86400000).toISOString();
      await saveDb();
      send(res, 200, 'ok');
      return;
    }

    send(res, 404, 'not found');
  } catch (e) {
    try { send(res, 500, 'server error: ' + e.message); } catch (e2) { }
  }
});

async function start() {
  await connectMongo();
  await loadDb();
  server.listen(PORT, function () {
    console.log('license server listening on :' + PORT);
    if (!ADMIN_KEY) console.log('WARNING: ADMIN_KEY env var is not set - /admin endpoints are locked until you set it.');
    if (!mongoColl) console.log('WARNING: running without MongoDB - data is NOT persistent across redeploys/restarts on free hosting.');
  });
}

start();
