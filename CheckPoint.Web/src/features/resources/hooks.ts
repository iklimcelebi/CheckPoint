import { useMutation, useQueryClient, type QueryClient } from '@tanstack/react-query'
import { useEffect, useRef } from 'react'
import type { Resource } from '../../api/types'
import { saveBlob } from '../../lib/download'
import { useAuth } from '../auth/authContext'
import { deleteResource, downloadResource, recordView, resourceKeys, setFavorite } from './api'

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
      await invalidateCatalogCounts(queryClient)
    },
  })
}

export function useToggleFavorite() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, favorite }: { id: number; favorite: boolean }) => setFavorite(id, favorite),
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: resourceKeys.all }),
        queryClient.invalidateQueries({ queryKey: MY_STATS_KEY }),
      ]),
  })
}

const MY_STATS_KEY = ['users', 'me', 'stats'] as const

export async function invalidateCatalogCounts(queryClient: QueryClient) {
  await Promise.all([
    queryClient.invalidateQueries({ queryKey: resourceKeys.all }),
    queryClient.invalidateQueries({ queryKey: ['universities'] }),
    queryClient.invalidateQueries({ queryKey: ['courses', 'popular'] }),
    queryClient.invalidateQueries({ queryKey: MY_STATS_KEY }),
  ])
}

export function useRecordView(id: number | undefined) {
  const queryClient = useQueryClient()
  const recorded = useRef<number | undefined>(undefined)

  useEffect(() => {
    if (id === undefined || recorded.current === id) return
    recorded.current = id
    void recordView(id).then(
      () =>
        Promise.all([
          queryClient.invalidateQueries({ queryKey: resourceKeys.all }),
          queryClient.invalidateQueries({ queryKey: ['courses', 'popular'] }),
        ]),
      () => undefined,
    )
  }, [id, queryClient])
}
