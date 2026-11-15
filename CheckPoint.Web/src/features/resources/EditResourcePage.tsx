import { zodResolver } from '@hookform/resolvers/zod'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate, useParams } from 'react-router'
import { ApiError } from '../../api/errors'
import type { Resource } from '../../api/types'
import { Page } from '../../components/Page'
import { Alert } from '../../components/ui/Alert'
import { Button } from '../../components/ui/Button'
import { buttonClasses } from '../../components/ui/buttonStyles'
import { SelectField } from '../../components/ui/SelectField'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/StatusViews'
import { TextAreaField } from '../../components/ui/TextAreaField'
import { TextField } from '../../components/ui/TextField'
import { formatFileSize } from '../../lib/format'
import { applyApiError } from '../../lib/forms'
import { parsePositiveInt } from '../../lib/params'
import { RESOURCE_TYPE_LABELS, RESOURCE_TYPES } from '../../lib/resourceTypes'
import { ForbiddenPage } from '../../routes/ForbiddenPage'
import { fetchResource, resourceKeys, updateResource } from './api'
import { useCanManage } from './hooks'
import { resourceEditSchema, type ResourceEditForm } from './schemas'

export function EditResourcePage() {
  const resourceId = parsePositiveInt(useParams().id)
  const query = useQuery({
    queryKey: resourceKeys.detail(resourceId ?? 0),
    queryFn: ({ signal }) => fetchResource(resourceId ?? 0, signal),
    enabled: resourceId !== undefined,
  })
  const canManage = useCanManage(query.data)

  if (resourceId === undefined || (query.error instanceof ApiError && query.error.status === 404)) {
    return (
      <Page title="Kaynak bulunamadı">
        <EmptyState title="Bu kaynak bulunamadı veya silinmiş olabilir.">
          <Link to="/kaynaklar" className="font-medium text-blue-700 hover:underline">
            Kaynaklara dön
          </Link>
        </EmptyState>
      </Page>
    )
  }
  if (query.isPending) return <LoadingState label="Kaynak yükleniyor…" />
  if (query.error) {
    return (
      <Page title="Kaynağı düzenle">
        <ErrorState error={query.error} onRetry={() => void query.refetch()} />
      </Page>
    )
  }
  if (!canManage) return <ForbiddenPage />

  return <EditResourceForm resource={query.data} />
}

function EditResourceForm({ resource }: { resource: Resource }) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [formError, setFormError] = useState<string>()
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting, isDirty },
  } = useForm<ResourceEditForm>({
    resolver: zodResolver(resourceEditSchema),
    defaultValues: {
      title: resource.title,
      description: resource.description ?? '',
      type: resource.type,
    },
  })

  const onSubmit = handleSubmit(async (values) => {
    setFormError(undefined)
    try {
      await updateResource(resource.id, {
        title: values.title,
        description: values.description || null,
        type: values.type,
      })
      await queryClient.invalidateQueries({ queryKey: resourceKeys.all })
      void navigate(`/kaynaklar/${resource.id}`, {
        replace: true,
        state: { notice: 'Değişiklikler kaydedildi.' },
      })
    } catch (error) {
      setFormError(applyApiError(error, setError, ['title', 'description', 'type']))
    }
  })

  return (
    <Page title="Kaynağı düzenle" description={resource.title}>
      <form
        noValidate
        onSubmit={(event) => void onSubmit(event)}
        className="space-y-5 rounded-lg border border-slate-200 bg-white p-6"
      >
        {formError && <Alert tone="error">{formError}</Alert>}

        <fieldset disabled={isSubmitting} className="space-y-5">
          <TextField
            label="Başlık"
            maxLength={200}
            error={errors.title?.message}
            {...register('title')}
          />
          <TextAreaField
            label="Açıklama (isteğe bağlı)"
            maxLength={2000}
            hint="En fazla 2000 karakter."
            error={errors.description?.message}
            {...register('description')}
          />
          <SelectField label="Tür" error={errors.type?.message} {...register('type')}>
            {RESOURCE_TYPES.map((type) => (
              <option key={type} value={type}>
                {RESOURCE_TYPE_LABELS[type]}
              </option>
            ))}
          </SelectField>
        </fieldset>

        <div className="rounded-md bg-slate-50 p-4 text-sm text-slate-700">
          <p className="mb-2 font-medium text-slate-800">Değiştirilemeyen bilgiler</p>
          <p>
            Ders: {resource.university.name} · {resource.course.code} {resource.course.name}
          </p>
          <p>
            Dosya: {resource.originalFileName} ({formatFileSize(resource.fileSize)})
          </p>
          <p className="mt-2 text-xs text-slate-500">
            Dosyayı veya dersi değiştirmek için kaynağı silip yeniden yükleyin.
          </p>
        </div>

        <div className="flex justify-end gap-2">
          <Link to={`/kaynaklar/${resource.id}`} className={buttonClasses('secondary')}>
            Vazgeç
          </Link>
          <Button
            type="submit"
            disabled={!isDirty}
            loading={isSubmitting}
            loadingText="Kaydediliyor…"
          >
            Kaydet
          </Button>
        </div>
      </form>
    </Page>
  )
}
