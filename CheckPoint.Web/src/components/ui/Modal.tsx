import { useEffect, useId, useRef, type ReactNode } from 'react'

interface ModalProps {
  open: boolean
  title: string
  busy?: boolean
  onClose: () => void
  children: (titleId: string) => ReactNode
}

export function Modal({ open, title, busy = false, onClose, children }: ModalProps) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  const titleId = useId()

  useEffect(() => {
    const dialog = dialogRef.current
    if (!dialog) return
    if (open && !dialog.open) dialog.showModal()
    if (!open && dialog.open) dialog.close()
  }, [open])

  return (
    <dialog
      ref={dialogRef}
      aria-labelledby={titleId}
      onCancel={(event) => {
        event.preventDefault()
        if (!busy) onClose()
      }}
      className="m-auto w-full max-w-md rounded-xl border border-slate-200 p-0 shadow-xl backdrop:bg-slate-900/40"
    >
      {open && (
        <div className="space-y-4 p-6">
          <h2 id={titleId} className="text-lg font-semibold text-slate-900">
            {title}
          </h2>
          {children(titleId)}
        </div>
      )}
    </dialog>
  )
}
