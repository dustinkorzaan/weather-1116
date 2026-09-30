import { useEffect, useLayoutEffect, useRef, useState } from 'react';
import { XIcon } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { findLastIndex } from '../../utils/array';
import { streamChatMessage } from '../../utils/chatStream';
import { useMapPins } from '../../map/mapPinsContext';
import { ChatMessage } from './ChatPanel';

const CHAT2A_ENDPOINT = '/Chat2a/messages';

// Chat2a panel docked beside the map (stacked under it below 640px), opened from the
// header. It stays mounted while hidden so the conversation survives open/close and
// route changes, and re-reads /User after every send so map pins follow
// AddUserCity/DeleteUserCity tool calls.
function Chat2aSidebar({ open, onClose }) {
  const { refreshCities } = useMapPins();
  const [input, setInput] = useState('');
  const [sending, setSending] = useState(false);
  const [history, setHistory] = useState([]);
  const sessionRef = useRef(null);
  const messagesRef = useRef(null);

  useLayoutEffect(() => {
    const element = messagesRef.current;
    if (element) {
      element.scrollTop = element.scrollHeight;
    }
  }, [history, open]);

  useEffect(() => {
    if (!open) {
      return undefined;
    }

    const onKeyDown = (event) => {
      if (event.key === 'Escape') {
        onClose();
      }
    };
    document.addEventListener('keydown', onKeyDown);
    return () => document.removeEventListener('keydown', onKeyDown);
  }, [open, onClose]);

  const sendMessage = async () => {
    const message = input.trim();
    if (!message || sending) return;

    let assistantText = '';
    let usage = null;

    setInput('');
    setSending(true);
    setHistory((current) => [...current, { role: 'user', content: message }]);

    try {
      await streamChatMessage({
        endpoint: CHAT2A_ENDPOINT,
        sessionId: sessionRef.current,
        message,
        onEvent: (payload) => {
          if (payload.type === 'session' && payload.sessionId) {
            sessionRef.current = payload.sessionId;
            return;
          }

          if (payload.type === 'token' && payload.text) {
            assistantText += payload.text;
            const snapshot = assistantText;
            setHistory((current) => {
              const next = [...current];
              const last = next[next.length - 1];
              if (last?.role === 'assistant' && last.streaming) {
                next[next.length - 1] = { role: 'assistant', content: snapshot, streaming: true };
              } else {
                next.push({ role: 'assistant', content: snapshot, streaming: true });
              }
              return next;
            });
            return;
          }

          if (payload.type === 'tool_start' && payload.toolName) {
            const { toolName, toolArguments } = payload;
            setHistory((current) => [
              ...current,
              { role: 'tool', content: `Running ${toolName} …`, toolName, toolArguments, running: true },
            ]);
            return;
          }

          if (payload.type === 'tool_end' && payload.toolName) {
            const { toolName, toolArguments, toolResult } = payload;
            setHistory((current) => {
              const next = [...current];
              const index = findLastIndex(
                next,
                (entry) => entry.role === 'tool' && entry.running && entry.toolName === toolName,
              );
              if (index === -1) return current;
              next[index] = {
                role: 'tool',
                content: `Ran ${toolName} …`,
                toolName,
                toolArguments: toolArguments || next[index].toolArguments,
                toolResult,
              };
              return next;
            });
            return;
          }

          if (payload.type === 'error' && payload.errorMessage) {
            setHistory((current) => [...current, { role: 'error', content: payload.errorMessage }]);
            return;
          }

          if (payload.type === 'done') {
            usage = payload.usage ?? null;
          }
        },
      });

      if (assistantText) {
        setHistory((current) => {
          const next = [...current];
          const last = next[next.length - 1];
          if (last?.role === 'assistant' && last.streaming) {
            next[next.length - 1] = { role: 'assistant', content: assistantText, usage };
          } else {
            next.push({ role: 'assistant', content: assistantText, usage });
          }
          return next;
        });
      }
    } catch (error) {
      setHistory((current) => [...current, { role: 'error', content: error.message || 'Chat failed.' }]);
    } finally {
      setSending(false);
      // The agent may have added or removed cities even when the send failed part-way.
      try {
        await refreshCities();
      } catch (err) {
        console.error('Failed to refresh cities:', err);
      }
    }
  };

  const onSubmit = async (event) => {
    event.preventDefault();
    await sendMessage();
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
      role="complementary"
      aria-label="Chat2a"
      hidden={!open}
      className="flex h-1/2 w-full shrink-0 flex-col border-t border-border bg-background text-foreground sm:h-auto sm:w-96 sm:border-t-0 sm:border-l"
    >
      <div className="flex items-center justify-between border-b border-border px-3 py-2">
        <h2 className="text-base font-semibold">Chat2a</h2>
        <Button
          type="button"
          variant="ghost"
          size="icon"
          aria-label="Close chat"
          title="Close chat"
          onClick={onClose}
        >
          <XIcon aria-hidden="true" />
        </Button>
      </div>

      <div
        ref={messagesRef}
        data-chat2a-sidebar-messages
        className="flex min-h-0 flex-1 flex-col gap-2 overflow-y-auto p-3"
      >
        {history.map((entry, index) => (
          <ChatMessage key={index} entry={entry} />
        ))}
      </div>

      <form className="flex flex-col gap-2 border-t border-border p-3" onSubmit={onSubmit}>
        <label className="sr-only" htmlFor="chat2a-sidebar-input">Message</label>
        <textarea
          id="chat2a-sidebar-input"
          className="w-full resize-y rounded-md border border-input bg-background px-2.5 py-2 text-foreground focus:border-ring focus:outline-none disabled:bg-muted"
          rows={3}
          value={input}
          placeholder="Ask Chat2a to add or remove a city…"
          onChange={(event) => setInput(event.target.value)}
          onKeyDown={onKeyDown}
          disabled={sending}
        />
        <Button
          className="self-end bg-primary px-4 py-2 text-primary-foreground shadow-sm hover:bg-primary/80"
          type="submit"
          disabled={sending}
        >
          {sending ? 'Sending…' : 'Send'}
        </Button>
      </form>
    </aside>
  );
}

export default Chat2aSidebar;
