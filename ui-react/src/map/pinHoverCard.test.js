import { afterEach, expect, test, vi } from 'vitest';
import {
  PIN_HOVER_CARD_BUTTON_LABEL,
  PIN_HOVER_CARD_CLOSE_DELAY_MS,
  bindPinHoverCard,
  createPinHoverCard,
} from './pinHoverCard';

afterEach(() => {
  vi.useRealTimers();
});

test('pin hover card shows the city name above the Get Current AI Weather button', () => {
  const { card, button } = createPinHoverCard('Atlanta, GA');

  expect(card.className).toMatch(/flex-col/);
  expect(card.querySelector('.weather-map-pin-card-name')?.textContent).toBe('Atlanta, GA');
  expect(button.textContent).toBe(PIN_HOVER_CARD_BUTTON_LABEL);
  expect(button.textContent).toBe('Get Current AI Weather');
  expect(card.firstElementChild).not.toBe(button);
  expect(card.lastElementChild).toBe(button);
});

test('hovering a pin opens the card; only the button requests weather', () => {
  const listeners = {};
  const marker = {
    addListener: vi.fn((eventName, handler) => {
      listeners[eventName] = handler;
    }),
  };
  const map = {
    addListener: vi.fn(),
  };
  const infoWindow = {
    open: vi.fn(),
    close: vi.fn(),
  };
  const maps = {
    InfoWindow: vi.fn(() => infoWindow),
  };
  const onGetWeather = vi.fn();

  const { button } = bindPinHoverCard({
    maps,
    map,
    marker,
    cityName: 'Toronto, ON',
    onGetWeather,
  });

  expect(marker.addListener).toHaveBeenCalledWith('mouseover', expect.any(Function));
  expect(marker.addListener).toHaveBeenCalledWith('mouseout', expect.any(Function));
  expect(marker.addListener).toHaveBeenCalledWith('click', expect.any(Function));

  listeners.mouseover();
  expect(infoWindow.open).toHaveBeenCalledWith({ map, anchor: marker });
  expect(onGetWeather).not.toHaveBeenCalled();

  listeners.click();
  expect(onGetWeather).not.toHaveBeenCalled();

  button.click();
  expect(onGetWeather).toHaveBeenCalledWith('Toronto, ON');
});

test('opening another pin closes the previous card', () => {
  const firstMarker = {
    addListener: vi.fn((eventName, handler) => {
      firstMarker[eventName] = handler;
    }),
  };
  const secondMarker = {
    addListener: vi.fn((eventName, handler) => {
      secondMarker[eventName] = handler;
    }),
  };
  const firstWindow = { open: vi.fn(), close: vi.fn() };
  const secondWindow = { open: vi.fn(), close: vi.fn() };
  const InfoWindow = vi.fn()
    .mockImplementationOnce(() => firstWindow)
    .mockImplementationOnce(() => secondWindow);
  const maps = { InfoWindow };
  const map = { addListener: vi.fn() };

  bindPinHoverCard({
    maps,
    map,
    marker: firstMarker,
    cityName: 'New York, NY',
    onGetWeather: vi.fn(),
  });
  bindPinHoverCard({
    maps,
    map,
    marker: secondMarker,
    cityName: 'Atlanta, GA',
    onGetWeather: vi.fn(),
  });

  firstMarker.mouseover();
  secondMarker.mouseover();

  expect(firstWindow.close).toHaveBeenCalled();
  expect(secondWindow.open).toHaveBeenCalled();
});

test('leaving a pin closes the card after a short delay', () => {
  vi.useFakeTimers();

  const listeners = {};
  const marker = {
    addListener: vi.fn((eventName, handler) => {
      listeners[eventName] = handler;
    }),
  };
  const infoWindow = {
    open: vi.fn(),
    close: vi.fn(),
  };

  bindPinHoverCard({
    maps: { InfoWindow: vi.fn(() => infoWindow) },
    map: { addListener: vi.fn() },
    marker,
    cityName: 'Charlotte, NC',
    onGetWeather: vi.fn(),
  });

  listeners.mouseover();
  listeners.mouseout();
  expect(infoWindow.close).not.toHaveBeenCalled();

  vi.advanceTimersByTime(PIN_HOVER_CARD_CLOSE_DELAY_MS);
  expect(infoWindow.close).toHaveBeenCalled();
});
