import { describe, expect, it } from 'vitest'
import { fileNameFromContentDisposition } from '../api/client'
import { ApiError, NETWORK_ERROR_STATUS } from '../api/errors'
import {
  fileExtension,
  MAX_FILE_SIZE_BYTES,
  validateFile,
  withExpectedContentType,
} from './fileRules'
import { formatDateTime, formatFileSize } from './format'
import { safeReturnPath } from './navigation'

describe('fileNameFromContentDisposition', () => {
  it('prefers the UTF-8 filename* parameter', () => {
    expect(
      fileNameFromContentDisposition(
        "attachment; filename=Calisma_Notlari.pdf; filename*=UTF-8''%C3%87al%C4%B1%C5%9Fma%20Notlar%C4%B1.pdf",
      ),
    ).toBe('Çalışma Notları.pdf')
  })

  it('falls back to the quoted filename parameter', () => {
    expect(fileNameFromContentDisposition('attachment; filename="rapor; v2.pdf"')).toBe(
      'rapor; v2.pdf',
    )
  })

  it('returns undefined without a header', () => {
    expect(fileNameFromContentDisposition(null)).toBeUndefined()
  })
})

describe('formatting', () => {
  it.each([
    [512, '512 B'],
    [2048, '2 KB'],
    [1536, '1,5 KB'],
    [5 * 1024 * 1024, '5 MB'],
  ])('formats %i bytes as %s', (bytes, expected) => {
    expect(formatFileSize(bytes)).toBe(expected)
  })

  it('shows UTC timestamps in Turkey time', () => {
    expect(formatDateTime('2026-11-15T09:30:00Z')).toBe('15 Kasım 2026 12:30')
  })
})

describe('safeReturnPath', () => {
  it.each([
    ['/kaynaklar/5', '/kaynaklar/5'],
    ['//evil.example', '/kaynaklar'],
    ['/\\evil.example', '/kaynaklar'],
    ['https://evil.example', '/kaynaklar'],
    [null, '/kaynaklar'],
  ])('maps %s to %s', (value, expected) => {
    expect(safeReturnPath(value)).toBe(expected)
  })
})

describe('validateFile', () => {
  const file = (name: string, size: number) =>
    new File([new Uint8Array(size)], name, { type: 'application/octet-stream' })

  it('accepts allowed files', () => {
    expect(validateFile(file('Notlar.PDF', 10))).toBeUndefined()
  })

  it.each([
    [undefined, 'Yüklenecek dosyayı seçin.'],
    [file('program.exe', 10), 'Bu dosya türü desteklenmiyor'],
    [file('bos.txt', 0), 'Boş dosya yüklenemez.'],
  ])('rejects invalid input', (input, message) => {
    expect(validateFile(input)).toContain(message)
  })

  it('rejects files over the size limit', () => {
    const big = { name: 'buyuk.txt', size: MAX_FILE_SIZE_BYTES + 1 } as File
    expect(validateFile(big)).toBe('Dosya 20 MB sınırını aşıyor.')
  })

  it('sets the content type expected by the API', () => {
    expect(fileExtension('Sunum.PPTX')).toBe('.pptx')
    expect(withExpectedContentType(file('sunum.pptx', 3)).type).toBe(
      'application/vnd.openxmlformats-officedocument.presentationml.presentation',
    )
  })
})

describe('ApiError', () => {
  it('uses the ProblemDetails detail as message', () => {
    const error = new ApiError(409, { status: 409, detail: 'Bu isimde bir üniversite zaten var.' })
    expect(error.message).toBe('Bu isimde bir üniversite zaten var.')
  })

  it('uses the first field error and finds errors case-insensitively', () => {
    const error = new ApiError(400, { errors: { Password: ['Parola zorunludur.'] } })
    expect(error.message).toBe('Parola zorunludur.')
    expect(error.fieldError('password')).toBe('Parola zorunludur.')
  })

  it('has a Turkish message for network failures', () => {
    expect(new ApiError(NETWORK_ERROR_STATUS).message).toContain('Sunucuya ulaşılamadı')
  })
})
