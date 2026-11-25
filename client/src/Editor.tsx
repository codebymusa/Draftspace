import { useEffect, useRef, useState } from 'react'
import { useEditor, useEditorState, EditorContent } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import { api, message, type Draft, type User } from './api'

export default function Editor({ draft, user, users }: { draft: Draft; user: number; users: User[] }) {
  const [title, setTitle] = useState(draft.title)
  const [status, setStatus] = useState('Saved')
  const [error, setError] = useState('')
  const [sharing, setSharing] = useState(false)
  const [revision, setRevision] = useState(0)
  const latest = useRef({ title: draft.title, revision: 0 })
  const saved = useRef(0)
  const pending = useRef<Promise<boolean> | null>(null)
  function changed(nextTitle?: string) {
    if (nextTitle !== undefined) latest.current.title = nextTitle
    setRevision(++latest.current.revision)
    setStatus('Unsaved changes')
  }
  const editor = useEditor({
    extensions: [StarterKit.configure({ heading: { levels: [1, 2, 3] }, blockquote: false, code: false, codeBlock: false, horizontalRule: false, link: false, strike: false })],
    content: draft.content,
    editorProps: { attributes: { class: 'writing', 'aria-label': 'Document content', role: 'textbox', 'aria-multiline': 'true' } },
    onUpdate: () => changed(),
  })
  const active = useEditorState({ editor, selector: ({ editor }) => ({
    bold: editor?.isActive('bold'), italic: editor?.isActive('italic'), underline: editor?.isActive('underline'),
    h1: editor?.isActive('heading', { level: 1 }), h2: editor?.isActive('heading', { level: 2 }),
    bullets: editor?.isActive('bulletList'), numbers: editor?.isActive('orderedList'),
  }) })
  async function save(): Promise<boolean> {
    while (pending.current) await pending.current
    if (!editor || saved.current === latest.current.revision) return true
    const snapshot = { ...latest.current }
    const body = JSON.stringify({ title: snapshot.title, content: editor.getJSON() })
    setStatus('Saving')
    setError('')
    const request = api<Draft>('documents/' + draft.id, user, { method: 'PUT', body }).then(() => {
      saved.current = snapshot.revision
      setStatus(saved.current === latest.current.revision ? 'Saved' : 'Unsaved changes')
      return true
    }).catch(e => {
      setStatus('Save failed')
      setError(message(e))
      return false
    }).finally(() => { pending.current = null })
    pending.current = request
    return request
  }

  useEffect(() => {
    if (!revision) return
    const timer = window.setTimeout(() => { void save() }, 700)
    return () => window.clearTimeout(timer)
  }, [revision])

  useEffect(() => {
    function warn(event: BeforeUnloadEvent) {
      if (latest.current.revision !== saved.current) event.preventDefault()
    }
    window.addEventListener('beforeunload', warn)
    return () => window.removeEventListener('beforeunload', warn)
  }, [])

  return <>
    <header className="top editor-top"><a className="button" href="#">â† Documents</a>
      <span className="save-status" role="status">{status}</span>
      {draft.isOwner && <button className="button" onClick={() => setSharing(true)}>Share</button>}
      <button className="primary" disabled={status === 'Saving' || status === 'Saved'} onClick={save}>Save</button>
    </header>
    {sharing && <ShareDialog draft={draft} user={user} users={users} close={() => setSharing(false)} />}
    <main className="editor-shell">
      {error && <p className="error" role="alert">{error}</p>}
      <div className="toolbar" role="group" aria-label="Formatting">
        <button aria-label="Bold" aria-pressed={active?.bold} onClick={() => editor?.chain().focus().toggleBold().run()}><b>B</b></button>
        <button aria-label="Italic" aria-pressed={active?.italic} onClick={() => editor?.chain().focus().toggleItalic().run()}><i>I</i></button>
        <button aria-label="Underline" aria-pressed={active?.underline} onClick={() => editor?.chain().focus().toggleUnderline().run()}><u>U</u></button>
        <button aria-label="Heading 1" aria-pressed={active?.h1} onClick={() => editor?.chain().focus().toggleHeading({ level: 1 }).run()}>H1</button>
        <button aria-label="Heading 2" aria-pressed={active?.h2} onClick={() => editor?.chain().focus().toggleHeading({ level: 2 }).run()}>H2</button>
        <button aria-label="Bullet list" aria-pressed={active?.bullets} onClick={() => editor?.chain().focus().toggleBulletList().run()}>â€¢ List</button>
        <button aria-label="Numbered list" aria-pressed={active?.numbers} onClick={() => editor?.chain().focus().toggleOrderedList().run()}>1. List</button>
      </div>
      <article className="paper">
        <input aria-label="Document title" className="title" value={title} maxLength={120} onChange={e => { setTitle(e.target.value); changed(e.target.value) }} />
        <EditorContent editor={editor} />
      </article>
    </main>
  </>
}


function ShareDialog({ draft, user, users, close }: { draft: Draft; user: number; users: User[]; close: () => void }) {
  const dialog = useRef<HTMLDialogElement>(null)
  const [shares, setShares] = useState<User[] | null>(null)
  const [selected, setSelected] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const path = 'documents/' + draft.id + '/shares'

  useEffect(() => {
    dialog.current?.showModal()
    const controller = new AbortController()
    api<User[]>(path, user, { signal: controller.signal }).then(setShares).catch(e => {
      if (!controller.signal.aborted) setError(message(e))
    })
    return () => controller.abort()
  }, [path, user])

  async function change(person: User, method: 'PUT' | 'DELETE') {
    setBusy(true)
    setError('')
    try {
      await api(path + '/' + person.id, user, { method })
      setShares(previous => method === 'PUT' ? [...(previous ?? []), person] : (previous ?? []).filter(p => p.id !== person.id))
      setSelected('')
    } catch (e) { setError(message(e)) }
    finally { setBusy(false) }
  }

  const available = users.filter(person => person.id !== user && !shares?.some(shared => shared.id === person.id))
  return <dialog ref={dialog} onCancel={close} aria-labelledby="share-title" className="share-dialog">
    <div className="flex items-center justify-between gap-3">
      <h2 id="share-title" className="text-lg font-semibold">Share document</h2>
      <button className="button" onClick={close}>Close</button>
    </div>
    <p className="my-3 text-sm text-slate-600">People you share with can edit this document.</p>
    {error && <p className="error" role="alert">{error}</p>}
    {shares === null ? !error && <p role="status">Loading access…</p> : <>
      {available.length > 0 && <form className="flex flex-wrap gap-2" onSubmit={e => {
        e.preventDefault()
        const person = available.find(p => String(p.id) === selected)
        if (person) void change(person, 'PUT')
      }}>
        <select aria-label="Demo user to share with" value={selected} onChange={e => setSelected(e.target.value)}>
          <option value="">Choose a demo user</option>
          {available.map(person => <option key={person.id} value={person.id}>{person.name}</option>)}
        </select>
        <button className="primary" disabled={!selected || busy}>Grant access</button>
      </form>}
      <ul className="mt-4 divide-y divide-slate-200">
        <li className="flex justify-between py-3 text-sm"><span>{draft.owner.name}</span><span>Owner</span></li>
        {shares.map(person => <li className="flex items-center justify-between py-2 text-sm" key={person.id}>
          <span>{person.name}</span><button className="button" disabled={busy} aria-label={'Remove access for ' + person.name} onClick={() => void change(person, 'DELETE')}>Remove access</button>
        </li>)}
      </ul>
    </>}
  </dialog>
}
