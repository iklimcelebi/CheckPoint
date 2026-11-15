import { API_BASE_URL } from '../lib/config'
import { clearSession, getSession } from '../lib/session'
import { ApiError, NETWORK_ERROR_STATUS, type ProblemDetails } from './errors'

type QueryValue = string | number | boolean | null | undefined

export interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE'
  query?: Record<string, QueryValue>
  json?: unknown
  body?: FormData
  signal?: AbortSignal
}

export function buildUrl(path: string, query?: Record<string, QueryValue>): string {
  const url = new URL(`${API_BASE_URL}${path}`)
  for (const [key, value] of Object.entries(query ?? {})) {
    if (value === undefined || value === null || value === '') continue
    url.searchParams.set(key, String(value))
  }
  return url.toString()
}

async function send(path: string, options: RequestOptions, accept: string): Promise<Response> {
  const headers = new Headers({ Accept: accept })
  const session = getSession()
  if (session) headers.set('Authorization', `Bearer ${session.token}`)

  let body: BodyInit | undefined = options.body
  if (options.json !== undefined) {
    headers.set('Content-Type', 'application/json')
    body = JSON.stringify(options.json)
  }

  let response: Response
  try {
    response = await fetch(buildUrl(path, options.query), {
      method: options.method ?? 'GET',
      headers,
      body,
      signal: options.signal,
    })
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') throw error
    throw new ApiError(NETWORK_ERROR_STATUS)
  }

  if (!response.ok) {
    const problem = await readProblem(response)
    if (response.status === 401 && session) clearSession('expired')
    throw new ApiError(response.status, problem)
  }

  return response
}

async function readProblem(response: Response): Promise<ProblemDetails | undefined> {
  const contentType = response.headers.get('Content-Type') ?? ''
  if (!contentType.includes('json')) return undefined
  try {
    return (await response.json()) as ProblemDetails
  } catch {
    return undefined
  }
}

export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const response = await send(path, options, 'application/json, application/problem+json')
  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}

export function apiUpload<T>(
  path: string,
  body: FormData,
  onProgress?: (percent: number) => void,
): Promise<T> {
  return new Promise<T>((resolve, reject) => {
    const session = getSession()
    const request = new XMLHttpRequest()
    request.open('POST', buildUrl(path))
    request.setRequestHeader('Accept', 'application/json, application/problem+json')
    if (session) request.setRequestHeader('Authorization', `Bearer ${session.token}`)

    request.upload.onprogress = (event) => {
      if (event.lengthComputable) onProgress?.(Math.round((event.loaded / event.total) * 100))
    }
    request.onerror = () => reject(new ApiError(NETWORK_ERROR_STATUS))
    request.onload = () => {
      const payload = parseJson(request.responseText)
      if (request.status >= 200 && request.status < 300) {
        resolve(payload as T)
        return
      }
      if (request.status === 401 && session) clearSession('expired')
      const isProblem = (request.getResponseHeader('Content-Type') ?? '').includes('json')
      reject(new ApiError(request.status, isProblem ? (payload as ProblemDetails) : undefined))
    }

    request.send(body)
  })
}

function parseJson(text: string): unknown {
  if (!text) return undefined
  try {
    return JSON.parse(text)
  } catch {
    return undefined
  }
}

export interface DownloadedFile {
  blob: Blob
  fileName: string
}

export async function apiDownload(path: string, signal?: AbortSignal): Promise<DownloadedFile> {
  const response = await send(path, { signal }, '*/*')
  return {
    blob: await response.blob(),
    fileName:
      fileNameFromContentDisposition(response.headers.get('Content-Disposition')) ?? 'dosya',
  }
}

export function fileNameFromContentDisposition(header: string | null): string | undefined {
  if (!header) return undefined

  const encoded = /filename\*\s*=\s*([^']*)'[^']*'([^;]+)/i.exec(header)
  const decoded = encoded?.[2] ? safeDecode(encoded[2].trim()) : undefined
  if (decoded) return decoded

  const plain = /filename\s*=\s*("((?:\\.|[^"\\])*)"|[^;]+)/i.exec(header)
  if (!plain) return undefined
  return (plain[2] ?? plain[1] ?? '').replace(/\\(.)/g, '$1').trim() || undefined
}

function safeDecode(value: string): string | undefined {
  try {
    return decodeURIComponent(value)
  } catch {
    return undefined
  }
}
