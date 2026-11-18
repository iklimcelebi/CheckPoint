import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useSearchParams } from 'react-router'
import { Alert } from '../../components/ui/Alert'
import { Button } from '../../components/ui/Button'
import { TextField } from '../../components/ui/TextField'
import { applyApiError } from '../../lib/forms'
import { RETURN_PARAM } from '../../lib/navigation'
import { getSignOutReason } from '../../lib/session'
import { useDocumentTitle } from '../../lib/useDocumentTitle'
import { login } from './api'
import { useAuth } from './authContext'
import { loginSchema, type LoginForm } from './schemas'

export function LoginPage() {
  useDocumentTitle('Giriş yap')
  const { signIn } = useAuth()
  const [searchParams] = useSearchParams()
  const [formError, setFormError] = useState<string>()
  const [expired] = useState(() => getSignOutReason() === 'expired')
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<LoginForm>({ resolver: zodResolver(loginSchema) })

  const onSubmit = handleSubmit(async (values) => {
    setFormError(undefined)
    try {
      signIn(await login(values))
    } catch (error) {
      setFormError(applyApiError(error, setError, ['email', 'password']))
    }
  })

  const returnPath = searchParams.get(RETURN_PARAM)
  const registerLink = returnPath
    ? `/kayit?${RETURN_PARAM}=${encodeURIComponent(returnPath)}`
    : '/kayit'

  return (
    <div className="space-y-5">
      <h1 className="text-xl font-semibold text-slate-900">Giriş yap</h1>
      {expired && !formError && (
        <Alert tone="warning">Oturumunuzun süresi doldu. Lütfen tekrar giriş yapın.</Alert>
      )}
      {formError && <Alert tone="error">{formError}</Alert>}

      <form noValidate onSubmit={(event) => void onSubmit(event)} className="space-y-4">
        <TextField
          label="E-posta"
          type="email"
          autoComplete="email"
          error={errors.email?.message}
          {...register('email')}
        />
        <TextField
          label="Parola"
          type="password"
          autoComplete="current-password"
          error={errors.password?.message}
          {...register('password')}
        />
        <Button
          type="submit"
          className="w-full"
          loading={isSubmitting}
          loadingText="Giriş yapılıyor…"
        >
          Giriş yap
        </Button>
      </form>

      <p className="text-sm text-slate-600">
        Hesabınız yok mu?{' '}
        <Link to={registerLink} className="font-medium text-accent hover:underline">
          Kayıt olun
        </Link>
      </p>
    </div>
  )
}
