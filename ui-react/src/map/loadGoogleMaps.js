let loadPromise = null;

/**
 * Loads the Google Maps JavaScript API once.
 * @param {string} apiKey
 * @returns {Promise<typeof google.maps>}
 */
export function loadGoogleMaps(apiKey) {
  if (typeof window === 'undefined') {
    return Promise.reject(new Error('Google Maps can only load in the browser.'));
  }

  if (window.google?.maps) {
    return Promise.resolve(window.google.maps);
  }

  if (loadPromise) {
    return loadPromise;
  }

  if (!apiKey) {
    return Promise.reject(new Error('Missing Google Maps API key.'));
  }

  loadPromise = new Promise((resolve, reject) => {
    const script = document.createElement('script');
    script.src = `https://maps.googleapis.com/maps/api/js?key=${encodeURIComponent(apiKey)}`;
    script.async = true;
    script.defer = true;
    script.onload = () => {
      if (window.google?.maps) {
        resolve(window.google.maps);
      } else {
        loadPromise = null;
        reject(new Error('Google Maps failed to initialize.'));
      }
    };
    script.onerror = () => {
      loadPromise = null;
      reject(new Error('Failed to load the Google Maps script.'));
    };
    document.head.appendChild(script);
  });

  return loadPromise;
}
