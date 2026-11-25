import type { JSONContent } from '@tiptap/react'

export type User = { id: number; name: string }
export type Summary = { id: string; title: string; updatedAt: string; owner: User; isOwner: boolean }
export type Draft = Summary & { content: JSONContent }

export async function api<T>(path: string, user?: number, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers)
  if (user) headers.set('X-Demo-User', String(user))
  if (typeof init?.body === 'string') headers.set('Content-Type', 'application/json')
  const response = await fetch('/api/' + path, { ...init, headers })
  if (!response.ok) {
    const problem = await response.json().catch(() => null)
    throw new Error(problem?.detail ?? problem?.title ?? 'DraftSpace could not complete the request.')
  }
  return response.status === 204 ? undefined as T : response.json()
}

export const message = (error: unknown) => error instanceof Error ? error.message : 'Something went wrong.'

