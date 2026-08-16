(function (window, document) {
  'use strict';

  function fullscreenElement() {
    return document.fullscreenElement || document.webkitFullscreenElement || null;
  }

  function isFullscreen(root) {
    return fullscreenElement() === root || root.classList.contains('is-css-fullscreen');
  }

  function syncButton(button, root) {
    const on = isFullscreen(root);
    button.setAttribute('aria-pressed', on ? 'true' : 'false');
    button.setAttribute('aria-label', on ? 'Exit fullscreen' : 'Enter fullscreen');
    const enterIcon = button.querySelector('[data-icon="enter"]');
    const exitIcon = button.querySelector('[data-icon="exit"]');
    if (enterIcon) {
      enterIcon.hidden = on;
    }
    if (exitIcon) {
      exitIcon.hidden = !on;
    }
  }

  function syncAll() {
    document.querySelectorAll('[data-chat-fullscreen-button]').forEach(function (button) {
      const root = button.closest('.chat-window');
      if (root) {
        syncButton(button, root);
      }
    });
  }

  function exitNative() {
    const exit = document.exitFullscreen || document.webkitExitFullscreen;
    if (fullscreenElement() && exit) {
      const result = exit.call(document);
      if (result && typeof result.catch === 'function') {
        result.catch(function () {});
      }
    }
  }

  function enter(root) {
    const req = root.requestFullscreen || root.webkitRequestFullscreen;
    if (!req) {
      root.classList.add('is-css-fullscreen');
      return;
    }

    const result = req.call(root);
    if (result && typeof result.catch === 'function') {
      result.catch(function () {
        root.classList.add('is-css-fullscreen');
        syncAll();
      });
    }
  }

  function exit(root) {
    exitNative();
    root.classList.remove('is-css-fullscreen');
  }

  function toggle(root) {
    if (isFullscreen(root)) {
      exit(root);
    } else {
      enter(root);
    }
  }

  function onClick(event) {
    const button = event.target.closest && event.target.closest('[data-chat-fullscreen-button]');
    if (!button) {
      return;
    }

    event.preventDefault();
    const root = button.closest('.chat-window');
    if (root) {
      toggle(root);
      syncAll();
    }
  }

  function onKeyDown(event) {
    if (event.key !== 'Escape') {
      return;
    }

    document.querySelectorAll('.chat-window.is-css-fullscreen').forEach(function (root) {
      root.classList.remove('is-css-fullscreen');
    });
    syncAll();
  }

  document.addEventListener('click', onClick);
  document.addEventListener('keydown', onKeyDown);
  document.addEventListener('fullscreenchange', syncAll);
  document.addEventListener('webkitfullscreenchange', syncAll);

  window.chatFullscreen = { toggle: toggle, syncAll: syncAll };
})(window, document);
