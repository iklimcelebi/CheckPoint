import { useState } from 'react'
import { errorMessage } from '../../api/errors'
import type { Resource } from '../../api/types'
import { Button } from '../../components/ui/Button'
import { ConfirmDialog } from '../../components/ui/ConfirmDialog'
import { useDeleteResource } from './hooks'

interface DeleteResourceButtonProps {
  resource: Resource
  onDeleted?: () => void
  size?: 'normal' | 'small'
}

export function DeleteResourceButton({
  resource,
  onDeleted,
  size = 'normal',
}: DeleteResourceButtonProps) {
  const [open, setOpen] = useState(false)
  const deletion = useDeleteResource()

  const close = () => {
    setOpen(false)
    deletion.reset()
  }

  return (
    <>
      <Button
        variant="danger"
        size={size}
        onClick={() => setOpen(true)}
        aria-label={size === 'small' ? `${resource.title} kaynağını sil` : undefined}
      >
        Sil
      </Button>
      <ConfirmDialog
        open={open}
        title="Kaynağı sil"
        confirmLabel="Sil"
        busyLabel="Siliniyor…"
        busy={deletion.isPending}
        error={deletion.error ? errorMessage(deletion.error) : undefined}
        onCancel={close}
        onConfirm={() =>
          deletion.mutate(resource.id, {
            onSuccess: () => {
              setOpen(false)
              onDeleted?.()
            },
          })
        }
      >
        <p>
          <strong>{resource.title}</strong> kaynağı ve dosyası kalıcı olarak silinecek. Bu işlem
          geri alınamaz.
        </p>
      </ConfirmDialog>
    </>
  )
}
