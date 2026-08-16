import { expect, test, vi } from 'vitest';
import {
  ADD_LOCATION_ERROR,
  ADD_LOCATION_LABEL,
  ADD_LOCATION_LOOKUP_LABEL,
  bindRightClickAddLocation,
  createAddLocationCard,
} from './rightClickAddLocation';

test('right-click card shows a + button labeled Add Location', () => {
  const { card, button, label } = createAddLocationCard();

  expect(card.getAttribute('aria-label')).toBe(ADD_LOCATION_LABEL);
  expect(button.getAttribute('aria-label')).toBe(ADD_LOCATION_LABEL);
  expect(label.textContent).toBe('Add Location');
  expect(card.querySelector('.weather-map-add-location-icon')?.innerHTML).toContain('M12 5v14');
  expect(card.querySelector('.weather-map-add-location-error')?.hidden).toBe(true);
});

test('right-clicking the map opens Add Location; clicking it reverse-looks up the point', () => {
  const listeners = {};
  const map = {
    addListener: vi.fn((eventName, handler) => {
      listeners[eventName] = handler;
      return { remove: vi.fn() };
    }),
  };
  const infoWindow = {
    open: vi.fn(),
    close: vi.fn(),
  };
  let createdOptions;
  function InfoWindow(options) {
    createdOptions = options;
    return infoWindow;
  }
  const onAddLocation = vi.fn();

  const cleanup = bindRightClickAddLocation({
    maps: { InfoWindow },
    map,
    onAddLocation,
  });

  expect(map.addListener).toHaveBeenCalledWith('rightclick', expect.any(Function));
  expect(map.addListener).toHaveBeenCalledWith('click', expect.any(Function));

  listeners.rightclick({
    latLng: {
      lat: () => 36.1627,
      lng: () => -86.7816,
    },
  });
  expect(infoWindow.open).toHaveBeenCalledWith({ map });
  expect(onAddLocation).not.toHaveBeenCalled();

  const button = createdOptions.content.querySelector('.weather-map-add-location-button');
  expect(button).toBeTruthy();
  button.click();
  expect(onAddLocation).toHaveBeenCalledWith(
    36.1627,
    -86.7816,
    expect.objectContaining({
      setBusy: expect.any(Function),
      setError: expect.any(Function),
      hide: expect.any(Function),
    })
  );

  cleanup();
});

test('add location control reports lookup progress and errors', () => {
  const { button, label, error, setBusy, setError } = createAddLocationCard();

  setBusy(true);
  expect(button.disabled).toBe(true);
  expect(button.getAttribute('aria-busy')).toBe('true');
  expect(label.textContent).toBe(ADD_LOCATION_LOOKUP_LABEL);

  setBusy(false);
  setError(ADD_LOCATION_ERROR);
  expect(button.disabled).toBe(false);
  expect(error.hidden).toBe(false);
  expect(error.textContent).toBe(ADD_LOCATION_ERROR);
});
