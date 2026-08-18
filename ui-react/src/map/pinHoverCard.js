export const PIN_HOVER_CARD_BUTTON_LABEL = 'Weather';
export const PIN_HOVER_CARD_CLOSE_DELAY_MS = 200;
export const PIN_HOVER_CARD_DELETE_LABEL = 'Remove from map';

const DELETE_ICON_SVG = `
  <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
    <path d="M3 6h18" />
    <path d="M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6" />
    <path d="M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2" />
    <line x1="10" x2="10" y1="11" y2="17" />
    <line x1="14" x2="14" y1="11" y2="17" />
  </svg>
`;

const SEARCH_ICON_SVG =
  '<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="11" cy="11" r="8" /><path d="m21 21-4.3-4.3" /></svg>';

let activeCloseCard = null;

/**
 * Builds the floating pin card: city name, delete, then Weather.
 * @param {string} cityName
 * @returns {{ card: HTMLDivElement, button: HTMLButtonElement, deleteButton: HTMLButtonElement }}
 */
export function createPinHoverCard(cityName) {
  const card = document.createElement('div');
  card.className = 'weather-map-pin-card flex flex-col gap-2 min-w-[10.5rem]';
  card.setAttribute('role', 'dialog');
  card.setAttribute('aria-label', cityName);

  const header = document.createElement('div');
  header.className = 'weather-map-pin-card-header flex items-center justify-between gap-2';

  const name = document.createElement('div');
  name.className = 'weather-map-pin-card-name text-sm font-semibold leading-none text-foreground';
  name.textContent = cityName;

  const deleteButton = document.createElement('button');
  deleteButton.type = 'button';
  deleteButton.className =
    'weather-map-pin-card-delete inline-flex size-6 shrink-0 cursor-pointer items-center justify-center rounded-md leading-none text-muted-foreground hover:bg-muted hover:text-destructive [&_svg]:block';
  deleteButton.setAttribute('aria-label', `Remove ${cityName} from the map`);
  deleteButton.innerHTML = DELETE_ICON_SVG;

  header.append(name, deleteButton);

  const button = document.createElement('button');
  button.type = 'button';
  button.className =
    'weather-map-pin-card-button inline-flex cursor-pointer items-center gap-1.5 rounded-md bg-primary px-2.5 py-1.5 text-left text-sm font-medium text-primary-foreground shadow-sm hover:bg-primary/80 [&_svg]:block';
  button.innerHTML = `${SEARCH_ICON_SVG}<span>${PIN_HOVER_CARD_BUTTON_LABEL}</span>`;

  card.append(header, button);
  return { card, button, deleteButton };
}

/**
 * Shows the pin card on hover (and tap). Weather and delete are explicit actions.
 * @param {{
 *   maps: { InfoWindow: new (opts: object) => object },
 *   map: object,
 *   marker: { addListener: (eventName: string, handler: Function) => void },
 *   cityName: string,
 *   onGetWeather: (cityName: string) => void,
 *   onDelete?: (cityName: string) => void,
 * }} options
 */
export function bindPinHoverCard({ maps, map, marker, cityName, onGetWeather, onDelete }) {
  const { card, button, deleteButton } = createPinHoverCard(cityName);
  const infoWindowOptions = {
    content: card,
    disableAutoPan: true,
    headerDisabled: true,
  };
  if (typeof maps.Size === 'function') {
    infoWindowOptions.pixelOffset = new maps.Size(0, -18);
  }
  const infoWindow = new maps.InfoWindow(infoWindowOptions);

  let closeTimer = null;
  let isOpen = false;

  function cancelClose() {
    if (closeTimer !== null) {
      clearTimeout(closeTimer);
      closeTimer = null;
    }
  }

  function openCard() {
    cancelClose();
    if (activeCloseCard && activeCloseCard !== closeCard) {
      activeCloseCard();
    }
    if (!isOpen) {
      infoWindow.open({ map, anchor: marker });
      isOpen = true;
    }
    activeCloseCard = closeCard;
  }

  function closeCard() {
    cancelClose();
    if (isOpen) {
      infoWindow.close();
      isOpen = false;
    }
    if (activeCloseCard === closeCard) {
      activeCloseCard = null;
    }
  }

  function scheduleClose() {
    cancelClose();
    closeTimer = setTimeout(closeCard, PIN_HOVER_CARD_CLOSE_DELAY_MS);
  }

  marker.addListener('mouseover', openCard);
  marker.addListener('mouseout', scheduleClose);
  marker.addListener('click', openCard);

  card.addEventListener('mouseenter', openCard);
  card.addEventListener('mouseleave', scheduleClose);
  card.addEventListener('click', (event) => {
    event.stopPropagation();
  });

  button.addEventListener('click', (event) => {
    event.preventDefault();
    event.stopPropagation();
    onGetWeather(cityName);
  });

  deleteButton.addEventListener('click', (event) => {
    event.preventDefault();
    event.stopPropagation();
    closeCard();
    onDelete?.(cityName);
  });

  if (map && typeof map.addListener === 'function') {
    map.addListener('click', closeCard);
  }

  return { infoWindow, card, button, deleteButton, openCard, closeCard };
}
