import type { FormEvent, ReactNode } from 'react'
import { Alert } from './Alert'
import { Button } from './Button'
import { Modal } from './Modal'

interface FormDialogProps {
  open: boolean
  title: string
  submitLabel: string
  busyLabel: string
  busy?: boolean
  error?: string
  onSubmit: (event: FormEvent<HTMLFormElement>) => void
  onClose: () => void
  children: ReactNode
}

export function FormDialog({
  open,
  title,
  submitLabel,
  busyLabel,
  busy = false,
  error,
  onSubmit,
  onClose,
  children,
}: FormDialogProps) {
  return (
    <Modal open={open} title={title} busy={busy} onClose={onClose}>
      {(titleId) => (
        <form noValidate aria-labelledby={titleId} onSubmit={onSubmit} className="space-y-4">
          {error && <Alert tone="error">{error}</Alert>}
          <fieldset disabled={busy} className="space-y-4">
            {children}
          </fieldset>
          <div className="flex justify-end gap-2">
            <Button variant="secondary" onClick={onClose} disabled={busy}>
              Vazgeç
            </Button>
            <Button type="submit" loading={busy} loadingText={busyLabel}>
              {submitLabel}
            </Button>
          </div>
        </form>
      )}
    </Modal>
  )
}
