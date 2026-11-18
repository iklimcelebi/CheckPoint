import { zodResolver } from '@hookform/resolvers/zod'
import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { errorMessage } from '../../api/errors'
import { Page } from '../../components/Page'
import { Alert } from '../../components/ui/Alert'
import { Button } from '../../components/ui/Button'
import { TextField } from '../../components/ui/TextField'
import { applyApiError } from '../../lib/forms'
import { changePassword, fetchCurrentUser } from './api'
import { changePasswordSchema, type ChangePasswordForm } from './schemas'

const ROLE_LABELS: Record<string, string> = {
  Admin: 'Yönetici',
  User: 'Kullanıcı',
}

export function ProfilePage() {
  const {
    data: user,
    isPending,
    error,
  } = useQuery({
    queryKey: ['users', 'me'],
    queryFn: ({ signal }) => fetchCurrentUser(signal),
  })

  return (
    <Page title="Profil" description="Hesap bilgileriniz.">
      {isPending && <p className="text-sm text-slate-600">Yükleniyor…</p>}
      {error && <Alert tone="error">{errorMessage(error)}</Alert>}
      {user && (
        <dl className="divide-y divide-slate-200 rounded-lg border border-slate-200 bg-white">
          {[
            ['Kullanıcı adı', user.username],
            ['E-posta', user.email],
            ['Rol', ROLE_LABELS[user.role] ?? user.role],
          ].map(([label, value]) => (
            <div key={label} className="grid gap-1 px-4 py-3 sm:grid-cols-3 sm:gap-4">
              <dt className="text-sm font-medium text-slate-600">{label}</dt>
              <dd className="text-sm text-slate-900 sm:col-span-2">{value}</dd>
            </div>
          ))}
        </dl>
      )}
      <ChangePasswordSection />
    </Page>
  )
}

const EMPTY_FORM: ChangePasswordForm = { currentPassword: '', newPassword: '', confirmPassword: '' }

function ChangePasswordSection() {
  const [formError, setFormError] = useState<string>()
  const [saved, setSaved] = useState(false)
  const {
    register,
    handleSubmit,
    setError,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ChangePasswordForm>({
    resolver: zodResolver(changePasswordSchema),
    defaultValues: EMPTY_FORM,
  })

  const onSubmit = handleSubmit(async ({ currentPassword, newPassword }) => {
    setFormError(undefined)
    setSaved(false)
    try {
      await changePassword({ currentPassword, newPassword })
      reset(EMPTY_FORM)
      setSaved(true)
    } catch (error) {
      setFormError(applyApiError(error, setError, ['currentPassword', 'newPassword']))
    }
  })

  return (
    <section
      aria-labelledby="change-password-title"
      className="space-y-4 rounded-lg border border-slate-200 bg-white p-6"
    >
      <h2 id="change-password-title" className="text-lg font-semibold text-slate-900">
        Parolayı değiştir
      </h2>
      {saved && (
        <Alert tone="success">
          Parolanız değiştirildi. Bir sonraki girişinizde yeni parolayı kullanın.
        </Alert>
      )}
      {formError && <Alert tone="error">{formError}</Alert>}
      <form noValidate onSubmit={(event) => void onSubmit(event)} className="grid max-w-md gap-4">
        <TextField
          label="Mevcut parola"
          type="password"
          autoComplete="current-password"
          error={errors.currentPassword?.message}
          {...register('currentPassword')}
        />
        <TextField
          label="Yeni parola"
          type="password"
          autoComplete="new-password"
          hint="8-64 karakter; en az bir harf ve bir rakam."
          error={errors.newPassword?.message}
          {...register('newPassword')}
        />
        <TextField
          label="Yeni parola (tekrar)"
          type="password"
          autoComplete="new-password"
          error={errors.confirmPassword?.message}
          {...register('confirmPassword')}
        />
        <div>
          <Button type="submit" loading={isSubmitting} loadingText="Kaydediliyor…">
            Parolayı değiştir
          </Button>
        </div>
      </form>
    </section>
  )
}
