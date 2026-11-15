export const MAX_FILE_SIZE_MB = 20
export const MAX_FILE_SIZE_BYTES = MAX_FILE_SIZE_MB * 1024 * 1024

export const ALLOWED_FILE_TYPES: Record<string, string> = {
  '.pdf': 'application/pdf',
  '.doc': 'application/msword',
  '.docx': 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  '.ppt': 'application/vnd.ms-powerpoint',
  '.pptx': 'application/vnd.openxmlformats-officedocument.presentationml.presentation',
  '.txt': 'text/plain',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.jpeg': 'image/jpeg',
}

export const ALLOWED_EXTENSIONS = Object.keys(ALLOWED_FILE_TYPES)

export function fileExtension(fileName: string): string {
  const dot = fileName.lastIndexOf('.')
  return dot >= 0 ? fileName.slice(dot).toLowerCase() : ''
}

export function validateFile(file: File | undefined): string | undefined {
  if (!file) return 'Yüklenecek dosyayı seçin.'
  if (!ALLOWED_FILE_TYPES[fileExtension(file.name)]) {
    return `Bu dosya türü desteklenmiyor. İzin verilenler: ${ALLOWED_EXTENSIONS.join(' ')}`
  }
  if (file.size === 0) return 'Boş dosya yüklenemez.'
  if (file.size > MAX_FILE_SIZE_BYTES) {
    return `Dosya ${MAX_FILE_SIZE_MB} MB sınırını aşıyor.`
  }
  return undefined
}

export function withExpectedContentType(file: File): File {
  const expected = ALLOWED_FILE_TYPES[fileExtension(file.name)]
  return expected && file.type !== expected ? new File([file], file.name, { type: expected }) : file
}
