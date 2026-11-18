import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router'
import { Page } from '../../components/Page'
import { Alert } from '../../components/ui/Alert'
import { Button } from '../../components/ui/Button'
import { TextField } from '../../components/ui/TextField'
import { applyApiError } from '../../lib/forms'
import { changePassword } from './api'
import { changePasswordSchema, type ChangePasswordForm } from './schemas'

const EMPTY_FORM: ChangePasswordForm = { currentPassword: '', newPassword: '', confirmPassword: '' }

export function ChangePasswordPage() {
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
    <Page
      title="Parolayı değiştir"
      description="Yeni parolanız bir sonraki girişinizde geçerli olur."
      actions={
        <Link to="/profil" className="text-sm font-medium text-accent hover:underline">
          ← Profile dön
        </Link>
      }
    >
      <div className="max-w-md space-y-4 rounded-lg border border-slate-200 bg-white p-6 shadow-sm">
        {saved && (
          <Alert tone="success">
            Parolanız değiştirildi. Bir sonraki girişinizde yeni parolayı kullanın.
          </Alert>
        )}
        {formError && <Alert tone="error">{formError}</Alert>}
        <form noValidate onSubmit={(event) => void onSubmit(event)} className="grid gap-4">
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
      </div>
    </Page>
  )
}
