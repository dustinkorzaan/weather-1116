export const ADD_LOCATION_LABEL = 'Add Location';
export const ADD_LOCATION_LOOKUP_LABEL = 'Looking up location…';
export const ADD_LOCATION_ERROR = 'Unable to find that location.';

const PLUS_ICON_SVG = `
  <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
    <path d="M12 5v14" />
    <path d="M5 12h14" />
  </svg>
`;

/**
 * Builds the right-click Add Location control: a + button with a label.
 * @returns {{
 *   card: HTMLDivElement,
 *   button: HTMLButtonElement,
 *   label: HTMLSpanElement,
 *   error: HTMLParagraphElement,
 *   setBusy: (busy: boolean) => void,
 *   setError: (message: string) => void,
 * }}
 */
export function createAddLocationCard() {
  const card = document.createElement('div');
  card.className = 'weather-map-add-location weather-map-pin-card flex flex-col gap-2 min-w-[10.5rem]';
  card.setAttribute('role', 'dialog');
  card.setAttribute('aria-label', ADD_LOCATION_LABEL);

  const button = document.createElement('button');
  button.type = 'button';
  button.className =
    'weather-map-add-location-button weather-map-pin-card-button inline-flex cursor-pointer items-center gap-2 rounded-md bg-primary px-2.5 py-1.5 text-left text-sm font-medium text-primary-foreground shadow-sm hover:bg-primary/80 disabled:cursor-default disabled:opacity-70';
  button.setAttribute('aria-label', ADD_LOCATION_LABEL);

  const icon = document.createElement('span');
  icon.className = 'weather-map-add-location-icon inline-flex size-4 shrink-0 items-center justify-center';
  icon.setAttribute('aria-hidden', 'true');
  icon.innerHTML = PLUS_ICON_SVG;

  const label = document.createElement('span');
  label.className = 'weather-map-add-location-label';
  label.textContent = ADD_LOCATION_LABEL;

  button.append(icon, label);

  const error = document.createElement('p');
  error.className = 'weather-map-add-location-error text-sm text-destructive';
  error.hidden = true;

  card.append(button, error);

  function setBusy(busy) {
    button.disabled = busy;
    button.setAttribute('aria-busy', busy ? 'true' : 'false');
    label.textContent = busy ? ADD_LOCATION_LOOKUP_LABEL : ADD_LOCATION_LABEL;
  }

  function setError(message) {
    const text = String(message || '').trim();
    error.textContent = text;
    error.hidden = !text;
  }

  return { card, button, label, error, setBusy, setError };
}

/**
 * Shows a + / Add Location control on map right-click. Clicking it reverse-geocodes
 * the clicked lat/long and adds a pin.
 * @param {{
 *   maps: { InfoWindow: new (opts: object) => object, Size?: new (w: number, h: number) => object },
 *   map: { addListener: (eventName: string, handler: Function) => object },
 *   onAddLocation: (lat: number, lng: number, controls: { setBusy: Function, setError: Function, hide: Function }) => void | Promise<void>,
 * }} options
 * @returns {() => void} cleanup
 */
export function bindRightClickAddLocation({ maps, map, onAddLocation }) {
  let infoWindow = null;
  let isOpen = false;

  function hide() {
    if (infoWindow && isOpen) {
      infoWindow.close();
    }
    isOpen = false;
    infoWindow = null;
  }

  function onKeyDown(event) {
    if (event.key === 'Escape') {
      hide();
    }
  }

  const rightClickListener = map.addListener('rightclick', (event) => {
    const latLng = event?.latLng;
    if (!latLng || typeof latLng.lat !== 'function' || typeof latLng.lng !== 'function') {
      return;
    }

    hide();

    const lat = latLng.lat();
    const lng = latLng.lng();
    const { card, button, setBusy, setError } = createAddLocationCard();
    const infoWindowOptions = {
      content: card,
      position: latLng,
      disableAutoPan: true,
      headerDisabled: true,
    };
    if (typeof maps.Size === 'function') {
      infoWindowOptions.pixelOffset = new maps.Size(12, -8);
    }
    infoWindow = new maps.InfoWindow(infoWindowOptions);
    infoWindow.open({ map });
    isOpen = true;

    card.addEventListener('click', (clickEvent) => {
      clickEvent.stopPropagation();
    });
    card.addEventListener('mousedown', (mouseEvent) => {
      mouseEvent.stopPropagation();
    });

    button.addEventListener('click', (clickEvent) => {
      clickEvent.preventDefault();
      clickEvent.stopPropagation();
      onAddLocation(lat, lng, { setBusy, setError, hide });
    });
  });

  const clickListener = map.addListener('click', hide);
  document.addEventListener('keydown', onKeyDown);

  return function cleanup() {
    hide();
    document.removeEventListener('keydown', onKeyDown);
    rightClickListener?.remove?.();
    clickListener?.remove?.();
  };
}
