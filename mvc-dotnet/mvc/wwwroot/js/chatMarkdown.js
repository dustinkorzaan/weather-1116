(function (window) {
  'use strict';

  function escapeText(markdown) {
    const el = document.createElement('div');
    el.textContent = markdown || '';
    return el.innerHTML;
  }

  function render(markdown) {
    if (!markdown) {
      return '';
    }
    if (!window.marked || typeof window.marked.parse !== 'function' || !window.DOMPurify) {
      return escapeText(markdown);
    }

    const html = window.marked.parse(String(markdown), { gfm: true, breaks: false });
    return window.DOMPurify.sanitize(html);
  }

  window.chatMarkdown = { render: render };
})(window);
