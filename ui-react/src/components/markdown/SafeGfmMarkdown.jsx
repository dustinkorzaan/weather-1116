import Markdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import rehypeSanitize from 'rehype-sanitize';

function SafeGfmMarkdown({ children }) {
  return (
    <Markdown remarkPlugins={[remarkGfm]} rehypePlugins={[rehypeSanitize]}>
      {children}
    </Markdown>
  );
}

export default SafeGfmMarkdown;
