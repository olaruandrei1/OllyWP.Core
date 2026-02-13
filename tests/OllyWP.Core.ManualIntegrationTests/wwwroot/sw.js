// OllyWP Service Worker
// Handles push notifications and click events

const SW_VERSION = '1.0.0';

console.log(`[OllyWP SW] Service Worker v${SW_VERSION} loaded`);

self.addEventListener('push', function(event) {
    console.log('[OllyWP SW] Push event received!');

    let data = {
        title: 'OllyWP',
        body: 'New notification',
        icon: null,
        badge: null,
        image: null,
        url: '/',
        tag: null,
        silent: false,
        data: {}
    };

    try {
        if (event.data) {
            const rawText = event.data.text();
            
            console.log('[OllyWP SW] Raw payload length:', rawText.length);
            console.log('[OllyWP SW] Raw payload:', rawText);

            const parsed = JSON.parse(rawText);
            
            console.log('[OllyWP SW] Parsed payload:', parsed);

            data = {
                title: parsed.title || data.title,
                body: parsed.body || parsed.message || data.body,
                icon: parsed.icon || data.icon,
                badge: parsed.badge || data.badge,
                image: parsed.image || data.image,
                url: parsed.url || data.url,
                tag: parsed.tag || null,
                silent: parsed.silent || data.silent,
                data: parsed.data || parsed.customData || data.data
            };
        } else {
            console.log('[OllyWP SW] No data in push event');
        }
    } catch (error) {
        console.error('[OllyWP SW] Failed to parse push data:', error);
        console.error('[OllyWP SW] Error details:', error.message);

        if (event.data) {
            try {
                console.log('[OllyWP SW] Raw bytes:', new Uint8Array(event.data.arrayBuffer()));
            } catch (e) {
                console.log('[OllyWP SW] Could not read as array buffer');
            }
        }
    }

    console.log('[OllyWP SW] Showing notification:', data.title);

    const options = {
        body: data.body,
        icon: data.icon,
        badge: data.badge,
        image: data.image,
        tag: data.tag,
        silent: data.silent,
        data: {
            url: data.url,
            ...data.data
        },
        requireInteraction: false,
        vibrate: [200, 100, 200]
    };

    Object.keys(options).forEach(key => {
        if (options[key] === null || options[key] === undefined) {
            delete options[key];
        }
    });

    console.log('[OllyWP SW] Notification options:', options);

    event.waitUntil(
        self.registration.showNotification(data.title, options)
            .then(() => {
                console.log('[OllyWP SW] Notification displayed successfully!');
            })
            .catch(err => {
                console.error('[OllyWP SW] Failed to show notification:', err);
            })
    );
});

self.addEventListener('notificationclick', function(event) {
    console.log('[OllyWP SW] Notification clicked!');

    event.notification.close();

    const url = event.notification.data?.url || '/';
    console.log('[OllyWP SW] Opening URL:', url);

    event.waitUntil(
        clients.matchAll({ type: 'window', includeUncontrolled: true })
            .then(windowClients => {
                for (const client of windowClients) {
                    if (client.url === url && 'focus' in client) {
                        return client.focus();
                    }
                }
                if (clients.openWindow) {
                    return clients.openWindow(url);
                }
            })
    );
});

self.addEventListener('notificationclose', function(event) {
    console.log('[OllyWP SW] Notification closed/dismissed');
});

self.addEventListener('pushsubscriptionchange', function(event) {
    console.log('[OllyWP SW] Push subscription changed!');
});

self.addEventListener('install', function(event) {
    console.log('[OllyWP SW] Installing...');
    self.skipWaiting();
});

self.addEventListener('activate', function(event) {
    console.log('[OllyWP SW] Activated!');
    event.waitUntil(self.clients.claim());
});

console.log('[OllyWP SW] Event listeners registered');