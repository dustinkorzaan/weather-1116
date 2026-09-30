import { useLayoutEffect, useRef, useState } from 'react';
import { X } from 'lucide-react';
import SafeGfmMarkdown from '../markdown/SafeGfmMarkdown';
import { Button } from '@/components/ui/button';
import { findLastIndex } from '../../utils/array';
import { formatToolHoverText } from '../../utils/chatToolHover';
import { streamChatMessage } from '../../utils/chatStream';
import { formatChatUsageChip, formatChatUsageDetails } from '../../utils/chatUsage';
import { useMapPins } from '../../map/mapPinsContext';
import { messageClasses, ToolChip } from './ChatPanel';

export const CHAT2A_ENDPOINT = '/Chat2a/messages';

// Right-hand Chat2a sidebar on the map page. Chat2a's agent can add and delete saved
// cities, so every completed turn re-reads /User and the map repaints its pins.
function Chat2aSidebar({ open, onClose }) {
  const { refreshCities } = useMapPins();
  const [input, setInput] = useState('');
  const [isSending, setIsSending] = useState(false);
  const [history, setHistory] = useState([]);
  const sessionIdRef = useRef(null);
  const messagesRef = useRef(null);
  const inputRef = useRef(null);

  useLayoutEffect(() => {
    if (messagesRef.current) {
      messagesRef.current.scrollTop = messagesRef.current.scrollHeight;
    }
  }, [history, open]);

  useLayoutEffect(() => {
    if (open) {
      inputRef.current?.focus();
    }
  }, [open]);

  const updateLastStreaming = (content, extra) => {
    setHistory((current) => {
      const next = [...current];
      const last = next[next.length - 1];
      const entry = { role: 'assistant', content, ...extra };
      if (last?.role === 'assistant' && last.streaming) {
        next[next.length - 1] = entry;
      } else {
        next.push(entry);
      }
      return next;
    });
  };

  const sendMessage = async () => {
    const message = input.trim();
    if (!message || isSending) return;

    let assistantText = '';
    let usage = null;

    setInput('');
    setIsSending(true);
    setHistory((current) => [...current, { role: 'user', content: message }]);

    try {
      await streamChatMessage({
        endpoint: CHAT2A_ENDPOINT,
        sessionId: sessionIdRef.current,
        message,
        onEvent: (payload) => {
          if (payload.type === 'session' && payload.sessionId) {
            sessionIdRef.current = payload.sessionId;
          } else if (payload.type === 'token' && payload.text) {
            assistantText += payload.text;
            updateLastStreaming(assistantText, { streaming: true });
          } else if (payload.type === 'tool_start' && payload.toolName) {
            const { toolName, toolArguments } = payload;
            setHistory((current) => [
              ...current,
              { role: 'tool', content: `Running ${toolName} …`, toolName, toolArguments, running: true },
            ]);
          } else if (payload.type === 'tool_end' && payload.toolName) {
            const { toolName, toolArguments, toolResult } = payload;
            setHistory((current) => {
              const index = findLastIndex(
                current,
                (entry) => entry.role === 'tool' && entry.running && entry.toolName === toolName,
              );
              if (index === -1) return current;
              const next = [...current];
              next[index] = {
                role: 'tool',
                content: `Ran ${toolName} …`,
                toolName,
                toolArguments: toolArguments || current[index].toolArguments,
                toolResult,
              };
              return next;
            });
          } else if ((payload.type === 'error' || payload.type === 'blocked') && payload.errorMessage) {
            setHistory((current) => [...current, { role: payload.type, content: payload.errorMessage }]);
          } else if (payload.type === 'done') {
            usage = payload.usage ?? null;
          }
        },
      });

      if (assistantText) {
        updateLastStreaming(assistantText, { usage });
      }
    } catch (error) {
      setHistory((current) => [...current, { role: 'error', content: error.message || 'Chat failed.' }]);
    } finally {
      setIsSending(false);
      // The agent may have called AddUserCity/DeleteUserCity; refetch so the map updates.
      refreshCities();
    }
  };

  const onSubmit = (event) => {
    event.preventDefault();
    void sendMessage();
  };

  const onKeyDown = (event) => {
    if (event.key === 'Enter' && !event.shiftKey && !event.isComposing && event.keyCode !== 229) {
      event.preventDefault();
      void sendMessage();
    }
  };

  return (
    <aside
      id="chat2a-sidebar"
      aria-label="Chat2a"
      hidden={!open}
      className="flex w-full max-w-sm shrink-0 flex-col border-l border-border bg-background max-sm:h-1/2 max-sm:max-w-none max-sm:border-l-0 max-sm:border-t"
    >
      <div className="flex items-center justify-between gap-2 border-b border-border px-3 py-2">
        <div className="min-w-0">
          <h2 className="text-base font-semibold">Chat</h2>
          <p className="truncate text-xs text-muted-foreground">Chat2a · Agent Framework · Local Loops</p>
        </div>
        <Button type="button" variant="ghost" size="icon" aria-label="Close chat" onClick={onClose}>
          <X aria-hidden="true" />
        </Button>
      </div>

      <div
        ref={messagesRef}
        data-chat-messages
        className="flex min-h-0 flex-1 flex-col gap-2 overflow-y-auto p-3"
        aria-live="polite"
      >
        {history.length === 0 ? (
          <p className="text-sm text-muted-foreground">
            Ask about the weather, or ask to add or remove a city on your map.
          </p>
        ) : null}
        {history.map((entry, index) =>
          entry.role === 'tool' ? (
            <ToolChip key={index} content={entry.content} details={formatToolHoverText(entry)} />
          ) : (
            <div key={index} className={messageClasses(entry)}>
              {entry.role === 'assistant' && !entry.streaming ? (
                <SafeGfmMarkdown>{entry.content}</SafeGfmMarkdown>
              ) : (
                entry.content
              )}
              {entry.role === 'assistant' && !entry.streaming && formatChatUsageChip(entry.usage) ? (
                <ToolChip
                  content={formatChatUsageChip(entry.usage)}
                  details={formatChatUsageDetails(entry.usage)}
                  className="mt-1.5 w-fit text-xs text-muted-foreground"
                />
              ) : null}
            </div>
          ),
        )}
      </div>

      <form className="flex flex-col gap-2 border-t border-border p-3" onSubmit={onSubmit}>
        <label className="sr-only" htmlFor="chat2a-sidebar-input">Message</label>
        <textarea
          ref={inputRef}
          id="chat2a-sidebar-input"
          className="w-full resize-none rounded-md border border-input bg-background px-2.5 py-2 text-foreground focus:border-ring focus:outline-none disabled:bg-muted"
          rows={3}
          value={input}
          placeholder="Add Nashville, TN to my map…"
          onChange={(event) => setInput(event.target.value)}
          onKeyDown={onKeyDown}
          disabled={isSending}
        />
        <Button
          className="self-end bg-primary px-4 py-2 text-primary-foreground shadow-sm hover:bg-primary/80"
          type="submit"
          disabled={isSending}
        >
          {isSending ? 'Sending…' : 'Send'}
        </Button>
      </form>
    </aside>
  );
}

export default Chat2aSidebar;
