import { zodResolver } from '@hookform/resolvers/zod'
import { useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { Link, useNavigate } from 'react-router'
import { Page } from '../../components/Page'
import { Alert } from '../../components/ui/Alert'
import { Button } from '../../components/ui/Button'
import { SelectField } from '../../components/ui/SelectField'
import { TextAreaField } from '../../components/ui/TextAreaField'
import { TextField } from '../../components/ui/TextField'
import { ALLOWED_EXTENSIONS, MAX_FILE_SIZE_MB, withExpectedContentType } from '../../lib/fileRules'
import { formatFileSize } from '../../lib/format'
import { applyApiError } from '../../lib/forms'
import { RESOURCE_TYPE_LABELS, RESOURCE_TYPES } from '../../lib/resourceTypes'
import { useCourseOptions } from '../courses/api'
import { useUniversityOptions } from '../universities/api'
import { createResource, resourceKeys } from './api'
import { resourceUploadSchema, type ResourceUploadForm } from './schemas'

const SERVER_FIELDS = ['title', 'description', 'universityId', 'courseId', 'type', 'file'] as const

export function UploadPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [progress, setProgress] = useState<number>()
  const [formError, setFormError] = useState<string>()
  const {
    register,
    handleSubmit,
    setError,
    resetField,
    control,
    formState: { errors, isSubmitting },
  } = useForm<ResourceUploadForm>({
    resolver: zodResolver(resourceUploadSchema),
    defaultValues: { title: '', description: '' },
  })

  const universityId = useWatch({ control, name: 'universityId' })
  const fileList = useWatch({ control, name: 'file' })
  const selectedFile = fileList?.[0]
  const universities = useUniversityOptions()
  const validUniversityId =
    Number.isInteger(universityId) && universityId > 0 ? universityId : undefined
  const courses = useCourseOptions(validUniversityId)

  const onSubmit = handleSubmit(async (values) => {
    setFormError(undefined)
    const file = values.file[0]
    if (!file) return
    setProgress(0)
    try {
      const created = await createResource(
        {
          title: values.title,
          description: values.description || undefined,
          universityId: values.universityId,
          courseId: values.courseId,
          type: values.type,
          file: withExpectedContentType(file),
        },
        setProgress,
      )
      await queryClient.invalidateQueries({ queryKey: resourceKeys.all })
      void navigate(`/kaynaklar/${created.id}`, { state: { notice: 'Kaynak yüklendi.' } })
    } catch (error) {
      setProgress(undefined)
      const message = applyApiError(error, setError, SERVER_FIELDS)
      setFormError(
        message ?? 'Bazı alanlar geçersiz. Lütfen işaretli alanları düzeltip tekrar deneyin.',
      )
    }
  })

  return (
    <Page
      title="Kaynak yükle"
      description="Ders notu, sunum, çalışma sorusu veya sınav sorusu paylaşın."
    >
      <form
        noValidate
        onSubmit={(event) => void onSubmit(event)}
        className="space-y-5 rounded-lg border border-slate-200 bg-white p-6"
      >
        {formError && <Alert tone="error">{formError}</Alert>}

        <fieldset disabled={isSubmitting} className="grid gap-5 sm:grid-cols-2">
          <TextField
            className="sm:col-span-2"
            label="Başlık"
            maxLength={200}
            error={errors.title?.message}
            {...register('title')}
          />
          <TextAreaField
            className="sm:col-span-2"
            label="Açıklama (isteğe bağlı)"
            maxLength={2000}
            hint="En fazla 2000 karakter."
            error={errors.description?.message}
            {...register('description')}
          />
          <SelectField
            label="Üniversite"
            defaultValue=""
            error={errors.universityId?.message}
            {...register('universityId', {
              valueAsNumber: true,
              onChange: () => resetField('courseId'),
            })}
          >
            <option value="" disabled>
              {universities.isPending ? 'Yükleniyor…' : 'Üniversite seçin'}
            </option>
            {universities.data?.map((university) => (
              <option key={university.id} value={university.id}>
                {university.name}
              </option>
            ))}
          </SelectField>
          <SelectField
            label="Ders"
            defaultValue=""
            disabled={!validUniversityId || courses.isPending}
            hint={
              validUniversityId
                ? courses.data?.length === 0
                  ? 'Bu üniversitede ders yok.'
                  : undefined
                : 'Önce üniversite seçin.'
            }
            error={errors.courseId?.message}
            {...register('courseId', { valueAsNumber: true })}
          >
            <option value="">
              {validUniversityId && courses.isPending ? 'Yükleniyor…' : 'Ders seçin'}
            </option>
            {courses.data?.map((course) => (
              <option key={course.id} value={course.id}>
                {course.code} — {course.name}
              </option>
            ))}
          </SelectField>
          <SelectField
            label="Tür"
            defaultValue=""
            error={errors.type?.message}
            {...register('type')}
          >
            <option value="" disabled>
              Tür seçin
            </option>
            {RESOURCE_TYPES.map((type) => (
              <option key={type} value={type}>
                {RESOURCE_TYPE_LABELS[type]}
              </option>
            ))}
          </SelectField>
          <TextField
            label="Dosya"
            type="file"
            accept={ALLOWED_EXTENSIONS.join(',')}
            hint={`${ALLOWED_EXTENSIONS.join(' ')} · en fazla ${MAX_FILE_SIZE_MB} MB`}
            error={errors.file?.message}
            {...register('file')}
          />
        </fieldset>

        {selectedFile && !errors.file && (
          <p className="text-sm text-slate-600">
            Seçilen dosya: <span className="break-all font-medium">{selectedFile.name}</span> (
            {formatFileSize(selectedFile.size)})
          </p>
        )}

        {progress !== undefined && (
          <div className="space-y-1">
            <div
              role="progressbar"
              aria-label="Yükleme ilerlemesi"
              aria-valuemin={0}
              aria-valuemax={100}
              aria-valuenow={progress}
              className="h-2 overflow-hidden rounded-full bg-slate-200"
            >
              <div
                className="h-full bg-blue-600 transition-all"
                style={{ width: `${progress}%` }}
              />
            </div>
            <p className="text-xs text-slate-600" aria-live="polite">
              {progress < 100 ? `Yükleniyor… %${progress}` : 'Dosya gönderildi, kaydediliyor…'}
            </p>
          </div>
        )}

        <div className="flex flex-wrap items-center justify-between gap-3">
          <p className="text-xs text-slate-500">
            Dersiniz listede yok mu?{' '}
            <Link to="/dersler" className="font-medium text-blue-700 hover:underline">
              Dersler sayfasından ekleyin.
            </Link>
          </p>
          <Button type="submit" loading={isSubmitting} loadingText="Yükleniyor…">
            Yükle
          </Button>
        </div>
      </form>
    </Page>
  )
}
