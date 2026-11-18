import { zodResolver } from '@hookform/resolvers/zod'
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router'
import { errorMessage } from '../../api/errors'
import type { Course, University } from '../../api/types'
import { Page } from '../../components/Page'
import { Alert } from '../../components/ui/Alert'
import { Button } from '../../components/ui/Button'
import { ConfirmDialog } from '../../components/ui/ConfirmDialog'
import { FormDialog } from '../../components/ui/FormDialog'
import { Pagination } from '../../components/ui/Pagination'
import { SelectField } from '../../components/ui/SelectField'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/StatusViews'
import { TextField } from '../../components/ui/TextField'
import { applyApiError } from '../../lib/forms'
import { useSearchState } from '../../lib/useSearchState'
import { useAuth } from '../auth/authContext'
import { useUniversityOptions } from '../universities/api'
import { createCourse, deleteCourse, fetchCourses, updateCourse } from './api'
import {
  courseCreateSchema,
  courseEditSchema,
  type CourseCreateForm,
  type CourseCreateFormInput,
  type CourseEditForm,
  type CourseEditFormInput,
} from './schemas'

const PAGE_SIZE = 10

type Editing = { mode: 'create' } | { mode: 'edit'; course: Course }

export function CoursesPage() {
  const queryClient = useQueryClient()
  const { searchInput, onSearchChange, q, page, setPage } = useSearchState()
  const [universityId, setUniversityId] = useState<number>()
  const [editing, setEditing] = useState<Editing | null>(null)
  const [notice, setNotice] = useState<string>()
  const [deleting, setDeleting] = useState<Course | null>(null)
  const { isAdmin } = useAuth()
  const universities = useUniversityOptions()

  const list = useQuery({
    queryKey: ['courses', 'list', { universityId, q, page }],
    queryFn: ({ signal }) =>
      fetchCourses({ universityId, q: q || undefined, page, pageSize: PAGE_SIZE }, signal),
    placeholderData: keepPreviousData,
  })

  const onSaved = async (message: string) => {
    setEditing(null)
    setNotice(message)
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['courses'] }),
      queryClient.invalidateQueries({ queryKey: ['resources'] }),
    ])
  }

  const deletion = useMutation({
    mutationFn: (course: Course) => deleteCourse(course.id),
    onSuccess: async (_, course) => {
      setDeleting(null)
      if (list.data?.items.length === 1 && page > 1) setPage(page - 1)
      await onSaved(`"${course.code} ${course.name}" silindi.`)
    },
  })

  return (
    <Page
      title="Dersler"
      description={
        isAdmin
          ? 'Dersleri ekleyin, düzenleyin veya silin.'
          : 'Derslere göz atın; listede olmayan dersi siz ekleyin.'
      }
      actions={
        <Button
          onClick={() => {
            setNotice(undefined)
            setEditing({ mode: 'create' })
          }}
        >
          Ders ekle
        </Button>
      }
    >
      {notice && <Alert tone="success">{notice}</Alert>}
      <div className="grid gap-4 sm:grid-cols-2">
        <SelectField
          label="Üniversite"
          value={universityId ?? ''}
          onChange={(event) => {
            setUniversityId(event.target.value ? Number(event.target.value) : undefined)
            setPage(1)
          }}
        >
          <option value="">Tüm üniversiteler</option>
          {universities.data?.map((university) => (
            <option key={university.id} value={university.id}>
              {university.name}
            </option>
          ))}
        </SelectField>
        <TextField
          label="Ara"
          type="search"
          placeholder="Ders adı veya kodu"
          maxLength={100}
          value={searchInput}
          onChange={(event) => onSearchChange(event.target.value)}
        />
      </div>

      {list.isPending ? (
        <LoadingState label="Dersler yükleniyor…" />
      ) : list.error ? (
        <ErrorState error={list.error} onRetry={() => void list.refetch()} />
      ) : list.data.items.length === 0 ? (
        <EmptyState
          title={q || universityId ? 'Aramanıza uyan ders bulunamadı.' : 'Henüz ders eklenmemiş.'}
        />
      ) : (
        <div className="space-y-4">
          <div className="overflow-x-auto rounded-lg border border-slate-200 bg-white">
            <table className="w-full text-left text-sm">
              <thead className="bg-slate-50 text-slate-600">
                <tr>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Kod
                  </th>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Ders
                  </th>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Üniversite
                  </th>
                  {isAdmin && (
                    <th scope="col" className="px-4 py-3 text-right font-medium">
                      İşlemler
                    </th>
                  )}
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200">
                {list.data.items.map((course) => (
                  <tr key={course.id}>
                    <td className="px-4 py-3 font-mono font-medium text-slate-900">
                      {course.code}
                    </td>
                    <td className="px-4 py-3">
                      <Link
                        to={`/kaynaklar?universite=${course.universityId}&ders=${course.id}`}
                        className="font-medium text-accent hover:underline"
                        title="Bu dersin kaynaklarını göster"
                      >
                        {course.name}
                      </Link>
                    </td>
                    <td className="px-4 py-3 text-slate-700">{course.universityName}</td>
                    {isAdmin && (
                      <td className="px-4 py-3">
                        <div className="flex justify-end gap-2">
                          <Button
                            variant="secondary"
                            size="small"
                            aria-label={`${course.code} ${course.name} düzenle`}
                            onClick={() => {
                              setNotice(undefined)
                              setEditing({ mode: 'edit', course })
                            }}
                          >
                            Düzenle
                          </Button>
                          <Button
                            variant="danger"
                            size="small"
                            aria-label={`${course.code} ${course.name} sil`}
                            onClick={() => {
                              setNotice(undefined)
                              deletion.reset()
                              setDeleting(course)
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

      {editing?.mode === 'create' && (
        <CreateCourseDialog
          universities={universities.data ?? []}
          initialUniversityId={universityId}
          onClose={() => setEditing(null)}
          onSaved={onSaved}
        />
      )}
      {isAdmin && editing?.mode === 'edit' && (
        <EditCourseDialog
          key={editing.course.id}
          course={editing.course}
          onClose={() => setEditing(null)}
          onSaved={onSaved}
        />
      )}
      <ConfirmDialog
        open={deleting !== null}
        title="Dersi sil"
        confirmLabel="Sil"
        busyLabel="Siliniyor…"
        busy={deletion.isPending}
        error={deletion.error ? errorMessage(deletion.error) : undefined}
        onCancel={() => setDeleting(null)}
        onConfirm={() => deleting && deletion.mutate(deleting)}
      >
        <p>
          <strong>
            {deleting?.code} {deleting?.name}
          </strong>{' '}
          silinecek. Kaynağı olan dersler silinemez.
        </p>
      </ConfirmDialog>
    </Page>
  )
}

const CODE_HINT = 'Kod kaydedilirken büyük harfe çevrilir.'

interface CreateCourseDialogProps {
  universities: University[]
  initialUniversityId?: number
  onClose: () => void
  onSaved: (message: string) => Promise<void>
}

function CreateCourseDialog({
  universities,
  initialUniversityId,
  onClose,
  onSaved,
}: CreateCourseDialogProps) {
  const [formError, setFormError] = useState<string>()
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<CourseCreateFormInput, unknown, CourseCreateForm>({
    resolver: zodResolver(courseCreateSchema),
    defaultValues: { name: '', code: '', universityId: initialUniversityId ?? Number.NaN },
  })

  const onSubmit = handleSubmit(async (values) => {
    setFormError(undefined)
    try {
      const created = await createCourse(values)
      await onSaved(`${created.code} — ${created.name} eklendi.`)
    } catch (error) {
      setFormError(applyApiError(error, setError, ['name', 'code', 'universityId']))
    }
  })

  return (
    <FormDialog
      open
      title="Ders ekle"
      submitLabel="Kaydet"
      busyLabel="Kaydediliyor…"
      busy={isSubmitting}
      error={formError}
      onClose={onClose}
      onSubmit={(event) => void onSubmit(event)}
    >
      <SelectField
        label="Üniversite"
        error={errors.universityId?.message}
        {...register('universityId', { valueAsNumber: true })}
      >
        <option value="">Üniversite seçin</option>
        {universities.map((university) => (
          <option key={university.id} value={university.id}>
            {university.name}
          </option>
        ))}
      </SelectField>
      <TextField
        label="Ders adı"
        maxLength={200}
        error={errors.name?.message}
        {...register('name')}
      />
      <TextField
        label="Ders kodu"
        maxLength={20}
        hint={CODE_HINT}
        className="[&_input]:uppercase"
        error={errors.code?.message}
        {...register('code')}
      />
    </FormDialog>
  )
}

interface EditCourseDialogProps {
  course: Course
  onClose: () => void
  onSaved: (message: string) => Promise<void>
}

function EditCourseDialog({ course, onClose, onSaved }: EditCourseDialogProps) {
  const [formError, setFormError] = useState<string>()
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<CourseEditFormInput, unknown, CourseEditForm>({
    resolver: zodResolver(courseEditSchema),
    defaultValues: { name: course.name, code: course.code },
  })

  const onSubmit = handleSubmit(async (values) => {
    setFormError(undefined)
    try {
      await updateCourse(course.id, values)
      await onSaved(`${values.code} — ${values.name} güncellendi.`)
    } catch (error) {
      setFormError(applyApiError(error, setError, ['name', 'code']))
    }
  })

  return (
    <FormDialog
      open
      title="Dersi düzenle"
      submitLabel="Kaydet"
      busyLabel="Kaydediliyor…"
      busy={isSubmitting}
      error={formError}
      onClose={onClose}
      onSubmit={(event) => void onSubmit(event)}
    >
      <p className="rounded-md bg-slate-50 p-3 text-sm text-slate-700">
        Üniversite: <strong>{course.universityName}</strong>
        <span className="block text-xs text-slate-500">Dersin üniversitesi değiştirilemez.</span>
      </p>
      <TextField
        label="Ders adı"
        maxLength={200}
        error={errors.name?.message}
        {...register('name')}
      />
      <TextField
        label="Ders kodu"
        maxLength={20}
        hint={CODE_HINT}
        className="[&_input]:uppercase"
        error={errors.code?.message}
        {...register('code')}
      />
    </FormDialog>
  )
}
