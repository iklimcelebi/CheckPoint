import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router'
import { Alert } from '../../components/ui/Alert'
import { Button } from '../../components/ui/Button'
import { TextField } from '../../components/ui/TextField'
import { applyApiError } from '../../lib/forms'
import { useDocumentTitle } from '../../lib/useDocumentTitle'
import { login, register as registerUser } from './api'
import { useAuth } from './authContext'
import { registerSchema, type RegisterForm } from './schemas'

export function RegisterPage() {
  useDocumentTitle('Kayıt ol')
  const { signIn } = useAuth()
  const [formError, setFormError] = useState<string>()
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<RegisterForm>({ resolver: zodResolver(registerSchema) })

  const onSubmit = handleSubmit(async ({ username, email, password }) => {
    setFormError(undefined)
    try {
      await registerUser({ username, email, password })
    } catch (error) {
      setFormError(applyApiError(error, setError, ['username', 'email', 'password']))
      return
    }

    try {
      signIn(await login({ email, password }))
    } catch {
      setFormError('Hesabınız oluşturuldu ancak otomatik giriş yapılamadı. Lütfen giriş yapın.')
    }
  })

  return (
    <div className="space-y-5">
      <h1 className="text-xl font-semibold text-slate-900">Kayıt ol</h1>
      {formError && <Alert tone="error">{formError}</Alert>}

      <form noValidate onSubmit={(event) => void onSubmit(event)} className="space-y-4">
        <TextField
          label="Kullanıcı adı"
          autoComplete="username"
          hint="3-50 karakter."
          error={errors.username?.message}
          {...register('username')}
        />
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
          autoComplete="new-password"
          hint="8-64 karakter; en az bir harf ve bir rakam."
          error={errors.password?.message}
          {...register('password')}
        />
        <TextField
          label="Parola (tekrar)"
          type="password"
          autoComplete="new-password"
          error={errors.confirmPassword?.message}
          {...register('confirmPassword')}
        />
        <Button
          type="submit"
          className="w-full"
          loading={isSubmitting}
          loadingText="Hesap oluşturuluyor…"
        >
          Kayıt ol
        </Button>
      </form>

      <p className="text-sm text-slate-600">
        Zaten hesabınız var mı?{' '}
        <Link to="/giris" className="font-medium text-accent hover:underline">
          Giriş yapın
        </Link>
      </p>
    </div>
  )
}
