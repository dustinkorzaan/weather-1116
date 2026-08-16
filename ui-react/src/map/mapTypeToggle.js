export const MAP_VIEW_TYPE = 'map';
export const AERIAL_VIEW_TYPE = 'aerial';

export const MAP_VIEW_LABEL = 'Map';
export const AERIAL_VIEW_LABEL = 'Aerial';
export const MAP_TYPE_TOGGLE_LABEL = 'Map view';

/**
 * Aerial uses hybrid: satellite imagery plus labels so city pins stay oriented.
 * Map uses the themed roadmap.
 */
export function resolveMapTypeId(maps, viewType) {
  const ids = maps?.MapTypeId;
  if (viewType === AERIAL_VIEW_TYPE) {
    return ids?.HYBRID ?? 'hybrid';
  }
  return ids?.ROADMAP ?? 'roadmap';
}

export function viewTypeFromMapTypeId(mapTypeId) {
  const id = String(mapTypeId || '').toLowerCase();
  if (id === 'hybrid' || id === 'satellite') {
    return AERIAL_VIEW_TYPE;
  }
  return MAP_VIEW_TYPE;
}

function createToggleButton(label, viewType) {
  const button = document.createElement('button');
  button.type = 'button';
  button.className = 'weather-map-type-toggle-button';
  button.dataset.viewType = viewType;
  button.textContent = label;
  return button;
}

function setPressed(button, pressed) {
  button.setAttribute('aria-pressed', pressed ? 'true' : 'false');
}

/**
 * Themed Map / Aerial control. Google's default type control is hidden so the
 * rest of the map chrome can stay custom.
 * @returns {{ element: HTMLDivElement, setViewType: (viewType: string) => void }}
 */
export function bindMapTypeToggle({ maps, map, initialViewType = MAP_VIEW_TYPE, onChange }) {
  const group = document.createElement('div');
  group.className = 'weather-map-type-toggle';
  group.setAttribute('role', 'group');
  group.setAttribute('aria-label', MAP_TYPE_TOGGLE_LABEL);

  const mapButton = createToggleButton(MAP_VIEW_LABEL, MAP_VIEW_TYPE);
  const aerialButton = createToggleButton(AERIAL_VIEW_LABEL, AERIAL_VIEW_TYPE);
  group.append(mapButton, aerialButton);

  function applyView(viewType, { skipMapUpdate } = {}) {
    const selected = viewType === AERIAL_VIEW_TYPE ? AERIAL_VIEW_TYPE : MAP_VIEW_TYPE;
    setPressed(mapButton, selected === MAP_VIEW_TYPE);
    setPressed(aerialButton, selected === AERIAL_VIEW_TYPE);
    if (!skipMapUpdate && typeof map.setMapTypeId === 'function') {
      map.setMapTypeId(resolveMapTypeId(maps, selected));
    }
    if (typeof onChange === 'function') {
      onChange(selected);
    }
  }

  mapButton.addEventListener('click', () => applyView(MAP_VIEW_TYPE));
  aerialButton.addEventListener('click', () => applyView(AERIAL_VIEW_TYPE));

  applyView(initialViewType);

  const position = maps?.ControlPosition?.TOP_LEFT;
  if (position != null && map.controls?.[position] && typeof map.controls[position].push === 'function') {
    map.controls[position].push(group);
  }

  return {
    element: group,
    setViewType: applyView,
  };
}
