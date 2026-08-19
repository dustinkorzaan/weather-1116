(function (window) {
  'use strict';

  function init() {
    var tabs = document.querySelectorAll('[data-ai-weather-tab]');
    var panels = document.querySelectorAll('[data-ai-weather-panel]');
    var descriptions = document.querySelectorAll('[data-ai-weather-description]');

    if (tabs.length === 0) {
      return;
    }

    function setActiveTab(tabId) {
      tabs.forEach(function (tab) {
        var isActive = tab.getAttribute('data-ai-weather-tab') === tabId;
        tab.classList.toggle('is-active', isActive);
        tab.setAttribute('aria-selected', isActive ? 'true' : 'false');
      });
      panels.forEach(function (panel) {
        var isActive = panel.getAttribute('data-ai-weather-panel') === tabId;
        if (isActive) {
          panel.removeAttribute('hidden');
        } else {
          panel.setAttribute('hidden', '');
        }
      });
      descriptions.forEach(function (desc) {
        var isActive = desc.getAttribute('data-ai-weather-description') === tabId;
        if (isActive) {
          desc.removeAttribute('hidden');
        } else {
          desc.setAttribute('hidden', '');
        }
      });
    }

    tabs.forEach(function (tab) {
      tab.addEventListener('click', function () {
        setActiveTab(tab.getAttribute('data-ai-weather-tab'));
      });
    });
  }

  window.currentAIWeatherTabs = { init: init };
})(window);
