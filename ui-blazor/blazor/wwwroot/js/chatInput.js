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
  var TOOL_HOVER_CLOSE_DELAY_MS = 200;
  var wrap = null;
  var card = null;
  var hideTimer = null;

  function cancelHide() {
    if (hideTimer !== null) {
      window.clearTimeout(hideTimer);
      hideTimer = null;
    }
  }

  function hide() {
    cancelHide();
    if (wrap) {
      wrap.hidden = true;
    }
  }

  function scheduleHide() {
    cancelHide();
    hideTimer = window.setTimeout(hide, TOOL_HOVER_CLOSE_DELAY_MS);
  }

  function ensureCard() {
    if (card) {
      return card;
    }

    wrap = document.createElement('div');
    wrap.className = 'chat-tool-hover-wrap';
    wrap.hidden = true;
    wrap.addEventListener('mouseenter', cancelHide);
    wrap.addEventListener('mouseleave', scheduleHide);

    card = document.createElement('pre');
    card.className = 'chat-tool-hover-card';
    card.setAttribute('role', 'tooltip');
    wrap.appendChild(card);
    document.body.appendChild(wrap);
    return card;
  }

  function show(anchor) {
    var text = anchor && anchor.getAttribute('data-tool-details');
    if (!text) {
      return;
    }

    cancelHide();
    var el = ensureCard();
    el.textContent = text;
    wrap.hidden = false;
    wrap.classList.remove('is-above');
    wrap.style.top = '';
    wrap.style.bottom = '';

    var rect = anchor.getBoundingClientRect();
    wrap.style.left = (rect.left + (rect.width / 2)) + 'px';
    wrap.style.top = rect.bottom + 'px';

    var wrapRect = wrap.getBoundingClientRect();
    if (wrapRect.bottom > window.innerHeight - 8) {
      wrap.classList.add('is-above');
      wrap.style.top = 'auto';
      wrap.style.bottom = (window.innerHeight - rect.top) + 'px';
      wrapRect = wrap.getBoundingClientRect();
    }
    if (wrapRect.right > window.innerWidth - 8) {
      wrap.style.left = (window.innerWidth - 8 - (wrapRect.width / 2)) + 'px';
    }
    if (wrapRect.left < 8) {
      wrap.style.left = (8 + (wrapRect.width / 2)) + 'px';
    }
  }

  function relatedIsHoverUi(related) {
    if (!related) {
      return false;
    }
    if (wrap && wrap.contains(related)) {
      return true;
    }
    return !!(related.closest && related.closest('[data-tool-details]'));
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

      if (relatedIsHoverUi(event.relatedTarget)) {
        return;
      }

      scheduleHide();
    });
    document.addEventListener('focusin', function (event) {
      var chip = event.target.closest && event.target.closest('[data-tool-details]');
      if (chip) {
        show(chip);
      }
    });
    document.addEventListener('focusout', function (event) {
      if (relatedIsHoverUi(event.relatedTarget)) {
        return;
      }
      scheduleHide();
    });
    window.addEventListener('scroll', function (event) {
      if (wrap && event.target && wrap.contains(event.target)) {
        return;
      }
      hide();
    }, true);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', bind);
  } else {
    bind();
  }

  return { show: show, hide: hide };
})();
