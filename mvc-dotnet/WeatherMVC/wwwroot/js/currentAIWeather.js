(function (window) {
  'use strict';

  function setHidden(el, hidden) {
    if (!el) {
      return;
    }
    if (hidden) {
      el.setAttribute('hidden', '');
    } else {
      el.removeAttribute('hidden');
    }
  }

  function parseSseChunk(chunk, onUpdate) {
    var lines = chunk.split('\n');
    for (var i = 0; i < lines.length; i += 1) {
      var line = lines[i];
      if (line.indexOf('data: ') !== 0) {
        continue;
      }

      var update = JSON.parse(line.slice(6));
      onUpdate(update);

      if (update.type === 'error') {
        throw new Error(update.message || 'Unable to load AI weather.');
      }
    }
  }

  function streamCurrentAIWeather(url, onUpdate) {
    return fetch(url, { headers: { Accept: 'text/event-stream' } })
      .then(function (response) {
        if (!response.ok || !response.body) {
          throw new Error('Unable to load AI weather.');
        }

        var reader = response.body.getReader();
        var decoder = new TextDecoder();
        var buffer = '';

        function readChunk() {
          return reader.read().then(function (result) {
            if (result.done) {
              if (buffer.trim()) {
                parseSseChunk(buffer, onUpdate);
              }
              return;
            }

            buffer += decoder.decode(result.value, { stream: true });
            var parts = buffer.split('\n\n');
            buffer = parts.pop() || '';

            for (var i = 0; i < parts.length; i += 1) {
              if (parts[i].trim()) {
                parseSseChunk(parts[i], onUpdate);
              }
            }

            return readChunk();
          });
        }

        return readChunk();
      });
  }

  function init(config) {
    var form = document.getElementById(config.formId);
    var locationInput = document.getElementById(config.locationId);
    var button = document.getElementById(config.buttonId);
    var spinner = document.getElementById(config.spinnerId);
    var errorEl = document.getElementById(config.errorId);
    var statusEl = document.getElementById(config.statusId);
    var previewEl = document.getElementById(config.previewId);
    var resultsEl = document.getElementById(config.resultsId);
    var summaryEl = document.getElementById(config.summaryId);
    var temperatureEl = document.getElementById(config.temperatureId);
    var windSpeedEl = document.getElementById(config.windSpeedId);
    var windDirectionEl = document.getElementById(config.windDirectionId);
    var conditionsEl = document.getElementById(config.conditionsId);

    if (!form || !locationInput || !button) {
      return;
    }

    form.addEventListener('submit', function (event) {
      event.preventDefault();

      var location = (locationInput.value || '').trim() || 'Nashville, TN';
      locationInput.value = location;

      setHidden(errorEl, true);
      setHidden(resultsEl, true);
      setHidden(spinner, false);
      if (statusEl) {
        statusEl.textContent = 'Starting AI weather request...';
        setHidden(statusEl, false);
      }
      if (previewEl) {
        previewEl.textContent = '';
        setHidden(previewEl, true);
      }

      button.disabled = true;
      button.setAttribute('aria-busy', 'true');
      locationInput.disabled = true;

      var url = config.endpoint + (config.endpoint.indexOf('?') >= 0 ? '&' : '?') +
        'location=' + encodeURIComponent(location);

      streamCurrentAIWeather(url, function (update) {
        if (update.type === 'status' && update.message && statusEl) {
          statusEl.textContent = update.message;
          setHidden(statusEl, false);
        }

        if (update.type === 'textDelta' && update.delta && previewEl) {
          previewEl.textContent += update.delta;
          setHidden(previewEl, false);
        }

        if (update.type === 'complete' && update.result) {
          summaryEl.textContent = update.result.fullSummary || '';
          temperatureEl.textContent = update.result.temperatureF;
          windSpeedEl.textContent = update.result.windSpeedMPH;
          windDirectionEl.textContent = update.result.windDirection || '';
          conditionsEl.textContent = update.result.conditions || '';
          setHidden(resultsEl, false);
          if (statusEl) {
            setHidden(statusEl, true);
          }
          if (previewEl) {
            setHidden(previewEl, true);
          }
        }
      })
        .catch(function (error) {
          errorEl.textContent = error.message || 'Unable to load AI weather.';
          setHidden(errorEl, false);
          if (statusEl) {
            setHidden(statusEl, true);
          }
          if (previewEl) {
            setHidden(previewEl, true);
          }
        })
        .finally(function () {
          setHidden(spinner, true);
          button.disabled = false;
          button.removeAttribute('aria-busy');
          locationInput.disabled = false;
        });
    });
  }

  window.currentAIWeather = { init: init };
})(window);
