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

  function scrollToBottom(element) {
    if (!element) {
      return;
    }

    element.scrollTop = element.scrollHeight;
  }

  return {
    attachEnterToSend: attachEnterToSend,
    tryAutoInit: tryAutoInit,
    scrollToBottom: scrollToBottom,
  };
})();

window.chatToolHover = (function () {
  var card = null;

  function ensureCard() {
    if (card) {
      return card;
    }

    card = document.createElement('pre');
    card.className = 'chat-tool-hover-card';
    card.setAttribute('role', 'tooltip');
    card.hidden = true;
    document.body.appendChild(card);
    return card;
  }

  function show(anchor) {
    var text = anchor && anchor.getAttribute('data-tool-details');
    if (!text) {
      return;
    }

    var el = ensureCard();
    el.textContent = text;
    el.hidden = false;
    var rect = anchor.getBoundingClientRect();
    el.style.left = (rect.left + (rect.width / 2)) + 'px';
    el.style.top = (rect.bottom + 8) + 'px';

    var cardRect = el.getBoundingClientRect();
    if (cardRect.bottom > window.innerHeight - 8) {
      el.style.top = Math.max(8, rect.top - cardRect.height - 8) + 'px';
    }
    if (cardRect.right > window.innerWidth - 8) {
      el.style.left = (window.innerWidth - 8 - (cardRect.width / 2)) + 'px';
    }
    if (cardRect.left < 8) {
      el.style.left = (8 + (cardRect.width / 2)) + 'px';
    }
  }

  function hide() {
    if (card) {
      card.hidden = true;
    }
  }

  function bind() {
    document.addEventListener('mouseover', function (event) {
      var chip = event.target.closest && event.target.closest('[data-tool-details]');
      if (chip) {
        show(chip);
      }
    });
    document.addEventListener('mouseout', function (event) {
      var chip = event.target.closest && event.target.closest('[data-tool-details]');
      if (!chip) {
        return;
      }

      var related = event.relatedTarget;
      if (related && chip.contains(related)) {
        return;
      }

      hide();
    });
    document.addEventListener('focusin', function (event) {
      var chip = event.target.closest && event.target.closest('[data-tool-details]');
      if (chip) {
        show(chip);
      }
    });
    document.addEventListener('focusout', hide);
    window.addEventListener('scroll', hide, true);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', bind);
  } else {
    bind();
  }

  return { show: show, hide: hide };
})();
