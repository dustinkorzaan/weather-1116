// Shared chat message rendering for the /chat-clients panel (chatClient.js) and the
// Chat2a sidebar (chatSidebar.js): markdown replies, usage chips and tool hover cards.
window.chatRender = (() => {
  const MESSAGE_ROLES = ['user', 'assistant', 'tool', 'error', 'blocked'];

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

  function renderEntry(entry, container) {
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
    container.appendChild(item);
    return item;
  }

  // One hover card per page, shared by every message list that attaches to it.
  const TOOL_HOVER_CLOSE_DELAY_MS = 200;
  let toolHoverWrap = null;
  let toolHoverCard = null;
  let toolHoverHideTimer = null;

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
      document.body.appendChild(toolHoverWrap);
    }

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
    positionToolHover(anchor);
  }

  // Shows the hover/focus card for tool lines and usage chips ([data-tool-details]) in messagesEl.
  function attachToolHover(messagesEl) {
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
  }

  return {
    renderEntry,
    formatChatUsageChip,
    formatChatUsageDetails,
    formatToolHoverText,
    attachToolHover,
  };
})();
