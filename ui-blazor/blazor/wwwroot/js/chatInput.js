window.chatInput = (function () {
  function isEnterToSend(event) {
    return event.key === 'Enter'
      && !event.shiftKey
      && !event.isComposing
      && event.keyCode !== 229;
  }

  function closestForm(el) {
    const form = el.closest && el.closest('form');
    if (form) {
      return form;
    }

    const root = el.getRootNode && el.getRootNode();
    const host = root && root.host;
    return host && host.closest ? host.closest('form') : null;
  }

  function attachEnterToSend(textarea) {
    if (!textarea || textarea.dataset.enterToSendAttached === 'true') {
      return;
    }

    textarea.dataset.enterToSendAttached = 'true';
    const form = closestForm(textarea);
    if (form && form.dataset.enterToSendSubmitGuarded !== 'true') {
      // Block native navigation if Enter fires before the Blazor circuit attaches.
      form.dataset.enterToSendSubmitGuarded = 'true';
      form.addEventListener('submit', function (event) {
        event.preventDefault();
      });
    }

    textarea.addEventListener('keydown', function (event) {
      if (!isEnterToSend(event)) {
        return;
      }

      event.preventDefault();
      if (form) {
        form.requestSubmit();
      }
    });
  }

  function collectTextareas(root) {
    const found = [];
    root.querySelectorAll('textarea.chat-input, .chat-input textarea').forEach((el) => found.push(el));
    root.querySelectorAll('.chat-input, fluent-text-area').forEach((el) => {
      const inner = el.shadowRoot && el.shadowRoot.querySelector('textarea');
      if (inner) {
        found.push(inner);
      }
    });
    return found;
  }

  function tryAutoInit(root) {
    collectTextareas(root).forEach(attachEnterToSend);
  }

  function startAutoInit() {
    tryAutoInit(document);

    if (window.MutationObserver) {
      const observer = new MutationObserver(function (mutations) {
        for (let i = 0; i < mutations.length; i++) {
          const mutation = mutations[i];
          if (mutation.addedNodes && mutation.addedNodes.length) {
            tryAutoInit(document);
            return;
          }
        }
      });
      observer.observe(document.documentElement, { childList: true, subtree: true });
    }

    window.setTimeout(function () {
      tryAutoInit(document);
    }, 500);
    window.setTimeout(function () {
      tryAutoInit(document);
    }, 2000);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', startAutoInit);
  } else {
    startAutoInit();
  }

  return { attachEnterToSend: attachEnterToSend, tryAutoInit: tryAutoInit };
})();
