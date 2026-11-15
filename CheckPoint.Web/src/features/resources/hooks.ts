import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { Resource } from '../../api/types'
import { saveBlob } from '../../lib/download'
import { useAuth } from '../auth/authContext'
import { deleteResource, downloadResource, resourceKeys } from './api'

export function useCanManage(resource: Resource | undefined): boolean {
  const { user, isAdmin } = useAuth()
  if (!resource || !user) return false
  return isAdmin || resource.uploadedBy.id === user.id
}

export function useDownloadResource() {
  return useMutation({
    mutationFn: async (resource: Resource) => {
      const file = await downloadResource(resource.id)
      saveBlob(file.blob, file.fileName || resource.originalFileName)
    },
  })
}

export function useDeleteResource() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => deleteResource(id),
    onSuccess: async (_, id) => {
      queryClient.removeQueries({ queryKey: resourceKeys.detail(id) })
      await queryClient.invalidateQueries({ queryKey: resourceKeys.all })
    },
  })
}
