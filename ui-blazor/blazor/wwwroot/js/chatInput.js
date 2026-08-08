window.chatInput = (function () {
  function attachEnterToSend(textarea) {
    if (!textarea || textarea.dataset.enterToSendAttached === 'true') {
      return;
    }

    textarea.dataset.enterToSendAttached = 'true';
    textarea.addEventListener('keydown', function (event) {
      if (event.key === 'Enter' && !event.shiftKey) {
        event.preventDefault();
        const form = textarea.closest('form');
        if (form) {
          form.requestSubmit();
        }
      }
    });
  }

  function tryAutoInit(root) {
    root.querySelectorAll('textarea.chat-input').forEach(attachEnterToSend);
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
