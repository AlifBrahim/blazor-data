function buildUrl(baseUrl, path) {
  try {
    return new URL(path, baseUrl).toString();
  } catch (error) {
    console.error('Failed to build URL', baseUrl, path, error);
    throw error;
  }
}

async function send(baseUrl, path, options) {
  const url = buildUrl(baseUrl, path);
  const response = await fetch(url, {
    credentials: 'include',
    ...options
  });

  const text = await response.text();
  return {
    ok: response.ok,
    status: response.status,
    body: text || null
  };
}

export async function getJson(baseUrl, path) {
  return await send(baseUrl, path, {
    method: 'GET',
    headers: {
      'Accept': 'application/json'
    }
  });
}

export async function postJson(baseUrl, path, payload) {
  return await send(baseUrl, path, {
    method: 'POST',
    headers: {
      'Accept': 'application/json',
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(payload)
  });
}
