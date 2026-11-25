import { useState } from 'react'
import { useEditor, useEditorState, EditorContent } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import { api, message, type Draft } from './api'

export function Editor({ draft, user }: { draft: Draft; user: number }) {
  const [title, setTitle] = useState(draft.title)
  const [status, setStatus] = useState('Saved')
  const [error, setError] = useState('')
  const editor = useEditor({
    extensions: [StarterKit.configure({ heading: { levels: [1, 2, 3] }, blockquote: false, code: false, codeBlock: false, horizontalRule: false, link: false, strike: false })],
    content: draft.content,
    editorProps: { attributes: { class: 'writing', 'aria-label': 'Document content', role: 'textbox', 'aria-multiline': 'true' } },
    onUpdate: () => setStatus('Unsaved changes'),
  })
  const active = useEditorState({ editor, selector: ({ editor }) => ({
    bold: editor?.isActive('bold'), italic: editor?.isActive('italic'), underline: editor?.isActive('underline'),
    h1: editor?.isActive('heading', { level: 1 }), h2: editor?.isActive('heading', { level: 2 }),
    bullets: editor?.isActive('bulletList'), numbers: editor?.isActive('orderedList'),
  }) })
  async function save() {
    if (!editor) return
    setStatus('Saving')
    setError('')
    try {
      await api<Draft>('documents/' + draft.id, user, { method: 'PUT', body: JSON.stringify({ title, content: editor.getJSON() }) })
      setStatus('Saved')
    } catch (e) { setStatus('Save failed'); setError(message(e)) }
  }

  return <>
    <header className="top editor-top"><a className="button" href="#">← Documents</a>
      <span className="save-status" role="status">{status}</span>
      <button className="primary" disabled={status === 'Saving'} onClick={save}>Save</button>
    </header>
    <main className="editor-shell">
      {error && <p className="error" role="alert">{error}</p>}
      <div className="toolbar" role="group" aria-label="Formatting">
        <button aria-label="Bold" aria-pressed={active?.bold} onClick={() => editor?.chain().focus().toggleBold().run()}><b>B</b></button>
        <button aria-label="Italic" aria-pressed={active?.italic} onClick={() => editor?.chain().focus().toggleItalic().run()}><i>I</i></button>
        <button aria-label="Underline" aria-pressed={active?.underline} onClick={() => editor?.chain().focus().toggleUnderline().run()}><u>U</u></button>
        <button aria-label="Heading 1" aria-pressed={active?.h1} onClick={() => editor?.chain().focus().toggleHeading({ level: 1 }).run()}>H1</button>
        <button aria-label="Heading 2" aria-pressed={active?.h2} onClick={() => editor?.chain().focus().toggleHeading({ level: 2 }).run()}>H2</button>
        <button aria-label="Bullet list" aria-pressed={active?.bullets} onClick={() => editor?.chain().focus().toggleBulletList().run()}>• List</button>
        <button aria-label="Numbered list" aria-pressed={active?.numbers} onClick={() => editor?.chain().focus().toggleOrderedList().run()}>1. List</button>
      </div>
      <article className="paper">
        <input aria-label="Document title" className="title" value={title} maxLength={120} onChange={e => { setTitle(e.target.value); setStatus('Unsaved changes') }} />
        <EditorContent editor={editor} />
      </article>
    </main>
  </>
}

