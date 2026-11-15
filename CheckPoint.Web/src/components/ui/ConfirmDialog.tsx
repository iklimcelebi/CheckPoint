import type { ReactNode } from 'react'
import { Alert } from './Alert'
import { Button } from './Button'
import { Modal } from './Modal'

interface ConfirmDialogProps {
  open: boolean
  title: string
  children: ReactNode
  confirmLabel: string
  busyLabel: string
  busy?: boolean
  error?: string
  onConfirm: () => void
  onCancel: () => void
}

export function ConfirmDialog({
  open,
  title,
  children,
  confirmLabel,
  busyLabel,
  busy = false,
  error,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  return (
    <Modal open={open} title={title} busy={busy} onClose={onCancel}>
      {() => (
        <>
          <div className="text-sm text-slate-700">{children}</div>
          {error && <Alert tone="error">{error}</Alert>}
          <div className="flex justify-end gap-2">
            <Button variant="secondary" onClick={onCancel} disabled={busy} autoFocus>
              Vazgeç
            </Button>
            <Button variant="danger" onClick={onConfirm} loading={busy} loadingText={busyLabel}>
              {confirmLabel}
            </Button>
          </div>
        </>
      )}
    </Modal>
  )
}
