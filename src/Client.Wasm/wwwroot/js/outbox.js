const dbName = 'capturedb';
const dbVersion = 1;

function openDb() {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open(dbName, dbVersion);

    request.onupgradeneeded = () => {
      const db = request.result;
      if (!db.objectStoreNames.contains('outbox')) {
        db.createObjectStore('outbox', { keyPath: 'id' });
      }
      if (!db.objectStoreNames.contains('settings')) {
        db.createObjectStore('settings', { keyPath: 'key' });
      }
    };

    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error);
  });
}

async function useStore(name, mode, handler) {
  const db = await openDb();
  return new Promise((resolve, reject) => {
    const tx = db.transaction(name, mode);
    const store = tx.objectStore(name);
    handler(store, resolve, reject);
    tx.onerror = () => reject(tx.error);
  });
}

export async function queueEntry(entry) {
  entry.enqueuedAt = new Date().toISOString();
  return useStore('outbox', 'readwrite', (store, resolve, reject) => {
    const request = store.put(entry);
    request.onsuccess = () => resolve(entry.id);
    request.onerror = () => reject(request.error);
  });
}

export async function dequeueEntry(id) {
  return useStore('outbox', 'readwrite', (store, resolve, reject) => {
    const request = store.delete(id);
    request.onsuccess = () => resolve();
    request.onerror = () => reject(request.error);
  });
}

export async function readPending() {
  return useStore('outbox', 'readonly', (store, resolve, reject) => {
    const results = [];
    const request = store.openCursor();
    request.onsuccess = () => {
      const cursor = request.result;
      if (cursor) {
        results.push(cursor.value);
        cursor.continue();
      } else {
        resolve(results);
      }
    };
    request.onerror = () => reject(request.error);
  });
}

export async function pendingCount() {
  return useStore('outbox', 'readonly', (store, resolve, reject) => {
    const request = store.count();
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error);
  });
}

export async function getSetting(key) {
  return useStore('settings', 'readonly', (store, resolve, reject) => {
    const request = store.get(key);
    request.onsuccess = () => {
      const record = request.result;
      resolve(record ? record.value : null);
    };
    request.onerror = () => reject(request.error);
  });
}

export async function setSetting(key, value) {
  return useStore('settings', 'readwrite', (store, resolve, reject) => {
    const request = store.put({ key, value });
    request.onsuccess = () => resolve(value);
    request.onerror = () => reject(request.error);
  });
}

export function subscribeOnlineStatus(dotnetRef) {
  const notify = () => dotnetRef.invokeMethodAsync('UpdateOnlineState', navigator.onLine);
  window.addEventListener('online', notify);
  window.addEventListener('offline', notify);
  notify();

  return {
    dispose: () => {
      window.removeEventListener('online', notify);
      window.removeEventListener('offline', notify);
    }
  };
}
