import { zodResolver } from '@hookform/resolvers/zod'
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { errorMessage } from '../../api/errors'
import type { University } from '../../api/types'
import { Page } from '../../components/Page'
import { Alert } from '../../components/ui/Alert'
import { Button } from '../../components/ui/Button'
import { ConfirmDialog } from '../../components/ui/ConfirmDialog'
import { FormDialog } from '../../components/ui/FormDialog'
import { Pagination } from '../../components/ui/Pagination'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/StatusViews'
import { TextField } from '../../components/ui/TextField'
import { applyApiError } from '../../lib/forms'
import { useSearchState } from '../../lib/useSearchState'
import { useAuth } from '../auth/authContext'
import { createUniversity, deleteUniversity, fetchUniversities, updateUniversity } from './api'
import { universitySchema, type UniversityForm } from './schemas'

const PAGE_SIZE = 10
const CATALOG_KEYS = [['universities'], ['courses'], ['resources']] as const

type Editing = { mode: 'create' } | { mode: 'edit'; university: University }

export function UniversitiesPage() {
  const { isAdmin } = useAuth()
  const queryClient = useQueryClient()
  const { searchInput, onSearchChange, q, page, setPage } = useSearchState()
  const [editing, setEditing] = useState<Editing | null>(null)
  const [deleting, setDeleting] = useState<University | null>(null)
  const [notice, setNotice] = useState<string>()

  const list = useQuery({
    queryKey: ['universities', 'list', { q, page }],
    queryFn: ({ signal }) =>
      fetchUniversities({ q: q || undefined, page, pageSize: PAGE_SIZE }, signal),
    placeholderData: keepPreviousData,
  })

  const refreshCatalog = () =>
    Promise.all(CATALOG_KEYS.map((queryKey) => queryClient.invalidateQueries({ queryKey })))

  const deletion = useMutation({
    mutationFn: (university: University) => deleteUniversity(university.id),
    onSuccess: async (_, university) => {
      setDeleting(null)
      setNotice(`"${university.name}" silindi.`)
      await refreshCatalog()
    },
  })

  return (
    <Page
      title="Üniversiteler"
      description={
        isAdmin ? 'Üniversiteleri ekleyin, düzenleyin veya silin.' : 'Kayıtlı üniversiteler.'
      }
      actions={
        isAdmin && (
          <Button
            onClick={() => {
              setNotice(undefined)
              setEditing({ mode: 'create' })
            }}
          >
            Üniversite ekle
          </Button>
        )
      }
    >
      {notice && <Alert tone="success">{notice}</Alert>}
      <TextField
        label="Ara"
        type="search"
        placeholder="Üniversite adı veya şehir"
        maxLength={100}
        className="max-w-md"
        value={searchInput}
        onChange={(event) => onSearchChange(event.target.value)}
      />

      {list.isPending ? (
        <LoadingState label="Üniversiteler yükleniyor…" />
      ) : list.error ? (
        <ErrorState error={list.error} onRetry={() => void list.refetch()} />
      ) : list.data.items.length === 0 ? (
        <EmptyState
          title={q ? 'Aramanıza uyan üniversite bulunamadı.' : 'Henüz üniversite eklenmemiş.'}
        />
      ) : (
        <div className="space-y-4">
          <div className="overflow-x-auto rounded-lg border border-slate-200 bg-white">
            <table className="w-full text-left text-sm">
              <thead className="bg-slate-50 text-slate-600">
                <tr>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Üniversite
                  </th>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Şehir
                  </th>
                  {isAdmin && (
                    <th scope="col" className="px-4 py-3 text-right font-medium">
                      İşlemler
                    </th>
                  )}
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200">
                {list.data.items.map((university) => (
                  <tr key={university.id}>
                    <td className="px-4 py-3 font-medium text-slate-900">{university.name}</td>
                    <td className="px-4 py-3 text-slate-700">{university.city}</td>
                    {isAdmin && (
                      <td className="px-4 py-3">
                        <div className="flex justify-end gap-2">
                          <Button
                            variant="secondary"
                            size="small"
                            aria-label={`${university.name} düzenle`}
                            onClick={() => {
                              setNotice(undefined)
                              setEditing({ mode: 'edit', university })
                            }}
                          >
                            Düzenle
                          </Button>
                          <Button
                            variant="danger"
                            size="small"
                            aria-label={`${university.name} sil`}
                            onClick={() => {
                              setNotice(undefined)
                              deletion.reset()
                              setDeleting(university)
                            }}
                          >
                            Sil
                          </Button>
                        </div>
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Pagination
            page={page}
            pageSize={PAGE_SIZE}
            totalCount={list.data.totalCount}
            onPageChange={setPage}
          />
        </div>
      )}

      {isAdmin && editing && (
        <UniversityFormDialog
          key={editing.mode === 'edit' ? editing.university.id : 'new'}
          editing={editing}
          onClose={() => setEditing(null)}
          onSaved={async (message) => {
            setEditing(null)
            setNotice(message)
            await refreshCatalog()
          }}
        />
      )}
      <ConfirmDialog
        open={deleting !== null}
        title="Üniversiteyi sil"
        confirmLabel="Sil"
        busyLabel="Siliniyor…"
        busy={deletion.isPending}
        error={deletion.error ? errorMessage(deletion.error) : undefined}
        onCancel={() => setDeleting(null)}
        onConfirm={() => deleting && deletion.mutate(deleting)}
      >
        <p>
          <strong>{deleting?.name}</strong> silinecek. Dersi olan üniversiteler silinemez.
        </p>
      </ConfirmDialog>
    </Page>
  )
}

interface UniversityFormDialogProps {
  editing: Editing
  onClose: () => void
  onSaved: (message: string) => Promise<void>
}

function UniversityFormDialog({ editing, onClose, onSaved }: UniversityFormDialogProps) {
  const [formError, setFormError] = useState<string>()
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<UniversityForm>({
    resolver: zodResolver(universitySchema),
    defaultValues:
      editing.mode === 'edit'
        ? { name: editing.university.name, city: editing.university.city }
        : { name: '', city: '' },
  })

  const onSubmit = handleSubmit(async (values) => {
    setFormError(undefined)
    try {
      if (editing.mode === 'edit') {
        await updateUniversity(editing.university.id, values)
        await onSaved(`"${values.name}" güncellendi.`)
      } else {
        await createUniversity(values)
        await onSaved(`"${values.name}" eklendi.`)
      }
    } catch (error) {
      setFormError(applyApiError(error, setError, ['name', 'city']))
    }
  })

  return (
    <FormDialog
      open
      title={editing.mode === 'edit' ? 'Üniversiteyi düzenle' : 'Üniversite ekle'}
      submitLabel="Kaydet"
      busyLabel="Kaydediliyor…"
      busy={isSubmitting}
      error={formError}
      onClose={onClose}
      onSubmit={(event) => void onSubmit(event)}
    >
      <TextField
        label="Üniversite adı"
        maxLength={200}
        error={errors.name?.message}
        {...register('name')}
      />
      <TextField label="Şehir" maxLength={100} error={errors.city?.message} {...register('city')} />
    </FormDialog>
  )
}
