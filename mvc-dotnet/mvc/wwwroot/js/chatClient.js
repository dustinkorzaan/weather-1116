(() => {
  const tabs = document.querySelectorAll('.chat-tab');
  const descriptions = document.querySelectorAll('.chat-tab-description');
  const messagesEl = document.getElementById('chat-messages');
  const form = document.getElementById('chat-form');
  const input = document.getElementById('chat-input');
  const sendButton = document.getElementById('chat-send');

  const MESSAGE_ROLES = ['user', 'assistant', 'tool', 'error'];

  let activeTab = 'Chat1a';
  const sessions = {
    Chat1a: null,
    Chat1b: null,
    Chat2a: null,
    Chat2b: null,
    Chat3: null,
  };

  window.chatHistory = window.chatHistory || {
    Chat1a: [],
    Chat1b: [],
    Chat2a: [],
    Chat2b: [],
    Chat3: [],
  };

  const sendingTabs = {
    Chat1a: false,
    Chat1b: false,
    Chat2a: false,
    Chat2b: false,
    Chat3: false,
  };

  function updateSendingControls() {
    const isSending = sendingTabs[activeTab];
    sendButton.disabled = isSending;
    sendButton.textContent = isSending ? 'Sending…' : 'Send';
    input.disabled = isSending;
  }

  function setActiveTab(tabId) {
    activeTab = tabId;
    tabs.forEach((tab) => {
      const isActive = tab.dataset.chatTab === tabId;
      tab.classList.toggle('is-active', isActive);
      tab.setAttribute('aria-selected', isActive ? 'true' : 'false');
    });
    descriptions.forEach((desc) => {
      if (desc.dataset.chatDescription === tabId) {
        desc.removeAttribute('hidden');
      } else {
        desc.setAttribute('hidden', '');
      }
    });
    renderMessages();
    updateSendingControls();
  }

  function scrollToBottom() {
    messagesEl.scrollTop = messagesEl.scrollHeight;
  }

  function requestScrollToBottom(tabId) {
    if (tabId === activeTab) {
      scrollToBottom();
    }
  }

  function renderMessages() {
    messagesEl.innerHTML = '';
    const history = window.chatHistory[activeTab] ?? [];
    history.forEach((entry) => renderEntry(entry));
    scrollToBottom();
  }

  function formatRunLogMs(ms) {
    return Number.isFinite(ms) ? Math.round(ms).toLocaleString() : '';
  }

  function formatRunLogTokenCount(tokens) {
    return Number.isFinite(tokens) ? Math.round(tokens).toLocaleString() : '';
  }

  function formatChatRuntime(ms) {
    if (!Number.isFinite(ms)) {
      return '';
    }

    const rounded = Math.round(ms);
    if (rounded < 1000) {
      return `${rounded}ms`;
    }

    let seconds = (rounded / 1000).toFixed(2);
    seconds = seconds.replace(/\.?0+$/, '');
    return `${seconds}s`;
  }

  function formatChatUsageChip(usage) {
    if (!usage) {
      return '';
    }

    const parts = [];
    const runtime = formatChatRuntime(usage.runtimeMs);
    if (runtime) {
      parts.push(runtime);
    }

    const tokens = formatRunLogTokenCount(usage.totalTokenCount);
    if (tokens) {
      parts.push(`${tokens} tok`);
    }

    return parts.join(' · ');
  }

  function formatChatUsageDetails(usage) {
    if (!usage) {
      return '';
    }

    const lines = [];
    if (Number.isFinite(usage.runtimeMs)) {
      lines.push(`Runtime: ${formatRunLogMs(usage.runtimeMs)} ms`);
    }

    [
      ['Input', usage.inputTokenCount],
      ['Cached', usage.cachedTokenCount],
      ['Output', usage.outputTokenCount],
      ['Reasoning', usage.reasoningTokenCount],
      ['Total', usage.totalTokenCount],
    ].forEach(([label, tokens]) => {
      const formatted = formatRunLogTokenCount(tokens);
      if (formatted) {
        lines.push(`${label}: ${formatted}`);
      }
    });
    return lines.join('\n');
  }

  function formatToolHoverText(entry) {
    const sections = [];
    if (entry.toolArguments) {
      sections.push(`Arguments\n${entry.toolArguments}`);
    }
    if (entry.toolResult) {
      sections.push(`Result\n${entry.toolResult}`);
    }
    if (sections.length === 0) {
      return entry.running ? 'Waiting for tool output…' : '';
    }
    return sections.join('\n\n');
  }

  function renderEntry(entry) {
    const item = document.createElement('div');
    const role = MESSAGE_ROLES.includes(entry.role) ? entry.role : 'assistant';
    item.className = `chat-message ${role}`;
    if (role === 'assistant' && !entry.streaming && window.safeGfmMarkdown) {
      item.classList.add('chat-markdown');
      item.innerHTML = window.safeGfmMarkdown.render(entry.content);
    } else {
      item.textContent = entry.content;
    }
    if (role === 'tool') {
      const details = formatToolHoverText(entry);
      if (details) {
        item.dataset.toolDetails = details;
        item.tabIndex = 0;
      }
    } else if (role === 'assistant' && !entry.streaming) {
      const chipText = formatChatUsageChip(entry.usage);
      const details = formatChatUsageDetails(entry.usage);
      if (chipText) {
        const chip = document.createElement('span');
        chip.className = 'chat-usage-chip';
        chip.textContent = chipText;
        if (details) {
          chip.dataset.toolDetails = details;
          chip.tabIndex = 0;
        }
        item.appendChild(chip);
      }
    }
    messagesEl.appendChild(item);
    return item;
  }

  // History is the single source of truth so tab switches never lose messages.
  function addEntry(tabId, entry) {
    window.chatHistory[tabId].push(entry);
    if (tabId === activeTab) {
      renderEntry(entry);
      scrollToBottom();
    }
    return entry;
  }

  function updateEntry(tabId, entry, content) {
    entry.content = content;
    if (tabId === activeTab) {
      renderMessages();
    }
  }

  tabs.forEach((tab) => {
    tab.addEventListener('click', () => setActiveTab(tab.dataset.chatTab));
  });

  function findLastRunningTool(history, toolName) {
    for (let index = history.length - 1; index >= 0; index -= 1) {
      const entry = history[index];
      if (entry.role === 'tool' && entry.running && entry.toolName === toolName) {
        return entry;
      }
    }

    return null;
  }

  async function streamChat(tabId, message) {
    const response = await fetch(`/${tabId}/Messages`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ sessionId: sessions[tabId], message }),
    });

    if (!response.ok || !response.body) {
      throw new Error(`Chat request failed (${response.status})`);
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = '';
    let assistantEntry = null;
    let assistantText = '';

    while (true) {
      const { done, value } = await reader.read();
      if (done) {
        if (assistantEntry) {
          assistantEntry.streaming = false;
          if (tabId === activeTab) {
            renderMessages();
          }
        }
        break;
      }

      buffer += decoder.decode(value, { stream: true });
      const parts = buffer.split('\n\n');
      buffer = parts.pop() ?? '';

      for (const part of parts) {
        const line = part.trim();
        if (!line.startsWith('data:')) continue;
        const payload = JSON.parse(line.slice(5).trim());

        if (payload.type === 'session' && payload.sessionId) {
          sessions[tabId] = payload.sessionId;
        } else if (payload.type === 'token' && payload.text) {
          assistantText += payload.text;
          if (!assistantEntry) {
            assistantEntry = addEntry(tabId, { role: 'assistant', content: assistantText, streaming: true });
          } else {
            updateEntry(tabId, assistantEntry, assistantText);
          }
        } else if (payload.type === 'tool_start' && payload.toolName) {
          addEntry(tabId, {
            role: 'tool',
            content: `Running ${payload.toolName} …`,
            toolName: payload.toolName,
            toolArguments: payload.toolArguments,
            running: true,
          });
        } else if (payload.type === 'tool_end' && payload.toolName) {
          const history = window.chatHistory[tabId];
          const pending = findLastRunningTool(history, payload.toolName);
          if (pending) {
            pending.running = false;
            pending.toolArguments = payload.toolArguments || pending.toolArguments;
            pending.toolResult = payload.toolResult;
            updateEntry(tabId, pending, `Ran ${payload.toolName} …`);
          }
        } else if (payload.type === 'error' && payload.errorMessage) {
          addEntry(tabId, { role: 'error', content: payload.errorMessage });
        } else if (payload.type === 'done') {
          if (assistantEntry) {
            assistantEntry.streaming = false;
            assistantEntry.usage = payload.usage || null;
            if (tabId === activeTab) {
              renderMessages();
            }
          }
          requestScrollToBottom(tabId);
        }
      }
    }
  }

  const TOOL_HOVER_CLOSE_DELAY_MS = 200;
  let toolHoverWrap = null;
  let toolHoverCard = null;
  let toolHoverHideTimer = null;
  let toolHoverLastAnchor = null;

  function cancelToolHoverHide() {
    if (toolHoverHideTimer !== null) {
      window.clearTimeout(toolHoverHideTimer);
      toolHoverHideTimer = null;
    }
  }

  function hideToolHover() {
    cancelToolHoverHide();
    if (toolHoverWrap) {
      toolHoverWrap.hidden = true;
    }
  }

  function scheduleToolHoverHide() {
    cancelToolHoverHide();
    toolHoverHideTimer = window.setTimeout(hideToolHover, TOOL_HOVER_CLOSE_DELAY_MS);
  }

  function relatedIsToolHoverUi(related) {
    if (!related) {
      return false;
    }
    if (toolHoverWrap && toolHoverWrap.contains(related)) {
      return true;
    }
    return !!(related.closest && related.closest('[data-tool-details]'));
  }

  function toolHoverFullscreenTarget() {
    return document.fullscreenElement || document.webkitFullscreenElement || document.body;
  }

  // A native-fullscreen chat window only paints its own subtree, so the
  // hover card must live inside it (not document.body) while active.
  function reparentToolHover() {
    if (!toolHoverWrap) {
      return;
    }

    const container = toolHoverFullscreenTarget();
    if (toolHoverWrap.parentNode !== container) {
      container.appendChild(toolHoverWrap);
    }
  }

  function ensureToolHoverCard() {
    if (!toolHoverCard) {
      toolHoverWrap = document.createElement('div');
      toolHoverWrap.id = 'chat-tool-hover-card';
      toolHoverWrap.className = 'chat-tool-hover-wrap';
      toolHoverWrap.hidden = true;
      toolHoverWrap.addEventListener('mouseenter', cancelToolHoverHide);
      toolHoverWrap.addEventListener('mouseleave', scheduleToolHoverHide);

      toolHoverCard = document.createElement('pre');
      toolHoverCard.className = 'chat-tool-hover-card';
      toolHoverCard.setAttribute('role', 'tooltip');
      toolHoverWrap.appendChild(toolHoverCard);
    }

    reparentToolHover();
    return toolHoverCard;
  }

  function positionToolHover(anchor) {
    toolHoverWrap.classList.remove('is-above');
    toolHoverWrap.style.top = '';
    toolHoverWrap.style.bottom = '';

    const rect = anchor.getBoundingClientRect();
    toolHoverWrap.style.left = `${rect.left + (rect.width / 2)}px`;
    toolHoverWrap.style.top = `${rect.bottom}px`;

    let wrapRect = toolHoverWrap.getBoundingClientRect();
    if (wrapRect.bottom > window.innerHeight - 8) {
      toolHoverWrap.classList.add('is-above');
      toolHoverWrap.style.top = 'auto';
      toolHoverWrap.style.bottom = `${window.innerHeight - rect.top}px`;
      wrapRect = toolHoverWrap.getBoundingClientRect();
    }
    if (wrapRect.right > window.innerWidth - 8) {
      toolHoverWrap.style.left = `${window.innerWidth - 8 - (wrapRect.width / 2)}px`;
    }
    if (wrapRect.left < 8) {
      toolHoverWrap.style.left = `${8 + (wrapRect.width / 2)}px`;
    }
  }

  function showToolHover(anchor) {
    const text = anchor && anchor.getAttribute('data-tool-details');
    if (!text) {
      return;
    }

    cancelToolHoverHide();
    const card = ensureToolHoverCard();
    card.textContent = text;
    toolHoverWrap.hidden = false;
    toolHoverLastAnchor = anchor;
    positionToolHover(anchor);
  }

  // Fires while the card is open (nothing else would move it) and while
  // it's hidden (so a stale reparented node doesn't linger in the former
  // fullscreen element after exiting).
  function onToolHoverFullscreenChange() {
    reparentToolHover();
    if (toolHoverWrap && !toolHoverWrap.hidden && toolHoverLastAnchor) {
      positionToolHover(toolHoverLastAnchor);
    }
  }

  document.addEventListener('fullscreenchange', onToolHoverFullscreenChange);
  document.addEventListener('webkitfullscreenchange', onToolHoverFullscreenChange);

  messagesEl.addEventListener('mouseover', (event) => {
    const chip = event.target.closest('[data-tool-details]');
    if (chip) {
      showToolHover(chip);
    }
  });
  messagesEl.addEventListener('mouseout', (event) => {
    const chip = event.target.closest('[data-tool-details]');
    if (!chip) {
      return;
    }
    if (relatedIsToolHoverUi(event.relatedTarget)) {
      return;
    }
    scheduleToolHoverHide();
  });
  messagesEl.addEventListener('focusin', (event) => {
    const chip = event.target.closest('[data-tool-details]');
    if (chip) {
      showToolHover(chip);
    }
  });
  messagesEl.addEventListener('focusout', (event) => {
    if (relatedIsToolHoverUi(event.relatedTarget)) {
      return;
    }
    scheduleToolHoverHide();
  });
  messagesEl.addEventListener('scroll', hideToolHover);

  input.addEventListener('keydown', (event) => {
    if (event.key === 'Enter' && !event.shiftKey && !event.isComposing && event.keyCode !== 229) {
      event.preventDefault();
      form.requestSubmit();
    }
  });

  form.addEventListener('submit', async (event) => {
    event.preventDefault();
    const message = input.value.trim();
    if (!message || sendingTabs[activeTab]) return;

    // The active tab can change while the stream is in flight.
    const tabId = activeTab;

    input.value = '';
    sendingTabs[tabId] = true;
    updateSendingControls();

    addEntry(tabId, { role: 'user', content: message });

    try {
      await streamChat(tabId, message);
    } catch (error) {
      addEntry(tabId, { role: 'error', content: error.message || 'Chat failed.' });
    } finally {
      sendingTabs[tabId] = false;
      updateSendingControls();
      requestScrollToBottom(tabId);
      input.focus();
    }
  });
})();
