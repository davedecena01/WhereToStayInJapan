/**
 * Vercel serverless proxy for Rakuten Travel API.
 *
 * Rakuten enforces IP-based access control tied to the registered application domain.
 * Requests from Railway's server IPs are blocked (403). This proxy runs on Vercel,
 * whose IPs are associated with where-to-stay-in-japan.vercel.app (the registered domain),
 * so Rakuten accepts the requests.
 *
 * Flow: Railway backend → POST /api/rakuten?<search params> → Rakuten API
 */
export default async function handler(req: any, res: any) {
  const secret = req.headers['x-proxy-secret'];
  if (!process.env['PROXY_SECRET'] || secret !== process.env['PROXY_SECRET']) {
    return res.status(401).json({ error: 'Unauthorized' });
  }

  const appId = process.env['RAKUTEN_APP_ID'];
  const accessKey = process.env['RAKUTEN_ACCESS_KEY'];
  if (!appId || !accessKey) {
    return res.status(500).json({ error: 'Proxy misconfigured: missing Rakuten credentials' });
  }

  // Forward all search params from the backend, then inject credentials
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(req.query as Record<string, string>)) {
    params.set(key, value);
  }
  params.set('applicationId', appId);
  params.set('accessKey', accessKey);
  params.set('format', 'json');

  const rakutenUrl =
    `https://openapi.rakuten.co.jp/engine/api/Travel/VacantHotelSearch/20170426?${params.toString()}`;

  try {
    const upstream = await fetch(rakutenUrl);
    const data = await upstream.json();
    return res.status(upstream.status).json(data);
  } catch {
    return res.status(502).json({ error: 'Rakuten upstream request failed' });
  }
}
