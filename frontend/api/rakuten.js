// Vercel serverless proxy for Rakuten Travel API.
// Rakuten enforces IP-based access control tied to the registered domain.
// Railway IPs are blocked (403). This proxy runs on Vercel (same IP range as
// the registered domain), so Rakuten accepts the requests.
const https = require('https');

module.exports = async function handler(req, res) {
  console.log('[rakuten-proxy] incoming request', req.method, Object.keys(req.query || {}).join(','));

  function json(status, body) {
    res.writeHead(status, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify(body));
  }

  const secret = req.headers['x-proxy-secret'];
  if (!process.env.PROXY_SECRET || secret !== process.env.PROXY_SECRET) {
    return json(401, { error: 'Unauthorized' });
  }

  const appId = process.env.RAKUTEN_APP_ID;
  const accessKey = process.env.RAKUTEN_ACCESS_KEY;
  if (!appId || !accessKey) {
    return json(500, { error: 'Proxy misconfigured: missing Rakuten credentials' });
  }

  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(req.query || {})) {
    params.set(key, Array.isArray(value) ? value[0] : String(value));
  }
  params.set('applicationId', appId);
  params.set('accessKey', accessKey);
  params.set('format', 'json');

  const rakutenPath =
    `/engine/api/Travel/VacantHotelSearch/20170426?${params.toString()}`;

  try {
    const { status, body } = await new Promise((resolve, reject) => {
      https.get({
        hostname: 'openapi.rakuten.co.jp',
        path: rakutenPath,
        headers: {
          'Referer': 'https://where-to-stay-in-japan.vercel.app/',
          'User-Agent': 'WhereToStayInJapan/1.0'
        }
      }, (upstream) => {
        let raw = '';
        upstream.on('data', (chunk) => { raw += chunk; });
        upstream.on('end', () => resolve({ status: upstream.statusCode, body: raw }));
      }).on('error', reject);
    });
    res.writeHead(status, { 'Content-Type': 'application/json' });
    res.end(body);
  } catch {
    json(502, { error: 'Rakuten upstream request failed' });
  }
};
