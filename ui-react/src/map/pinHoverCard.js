export const PIN_HOVER_CARD_BUTTON_LABEL = 'Get Current AI Weather';
export const PIN_HOVER_CARD_CLOSE_DELAY_MS = 200;

let activeCloseCard = null;

/**
 * Builds the floating pin card: city name, then a Get Current AI Weather button.
 * @param {string} cityName
 * @returns {{ card: HTMLDivElement, button: HTMLButtonElement }}
 */
export function createPinHoverCard(cityName) {
  const card = document.createElement('div');
  card.className = 'weather-map-pin-card flex flex-col gap-2 min-w-[10.5rem]';
  card.setAttribute('role', 'dialog');
  card.setAttribute('aria-label', cityName);

  const name = document.createElement('div');
  name.className = 'weather-map-pin-card-name text-sm font-semibold text-foreground';
  name.textContent = cityName;

  const button = document.createElement('button');
  button.type = 'button';
  button.className =
    'weather-map-pin-card-button cursor-pointer rounded-md bg-primary px-2.5 py-1.5 text-left text-sm font-medium text-primary-foreground shadow-sm hover:bg-primary/80';
  button.textContent = PIN_HOVER_CARD_BUTTON_LABEL;

  card.append(name, button);
  return { card, button };
}

/**
 * Shows the pin card on hover (and tap). Only the button navigates.
 * @param {{
 *   maps: { InfoWindow: new (opts: object) => object },
 *   map: object,
 *   marker: { addListener: (eventName: string, handler: Function) => void },
 *   cityName: string,
 *   onGetWeather: (cityName: string) => void,
 * }} options
 */
export function bindPinHoverCard({ maps, map, marker, cityName, onGetWeather }) {
  const { card, button } = createPinHoverCard(cityName);
  const infoWindow = new maps.InfoWindow({
    content: card,
    disableAutoPan: true,
    headerDisabled: true,
  });

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

  if (map && typeof map.addListener === 'function') {
    map.addListener('click', closeCard);
  }

  return { infoWindow, card, button, openCard, closeCard };
}
