import { useId, type ComponentProps } from 'react'

interface TextAreaFieldProps extends ComponentProps<'textarea'> {
  label: string
  error?: string
  hint?: string
}

export function TextAreaField({
  label,
  error,
  hint,
  id,
  className,
  ...textAreaProps
}: TextAreaFieldProps) {
  const generatedId = useId()
  const fieldId = id ?? generatedId
  const hintId = hint ? `${fieldId}-hint` : undefined
  const errorId = error ? `${fieldId}-error` : undefined
  const describedBy = [hintId, errorId].filter(Boolean).join(' ') || undefined

  return (
    <div className={className}>
      <label htmlFor={fieldId} className="mb-1 block text-sm font-medium text-slate-800">
        {label}
      </label>
      <textarea
        id={fieldId}
        rows={4}
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy}
        className={[
          'block w-full rounded-md border bg-white px-3 py-2 text-sm text-slate-900 shadow-sm',
          'focus:outline-2 focus:outline-offset-0',
          error ? 'border-red-500 focus:outline-red-500' : 'border-slate-300 focus:outline-accent',
        ].join(' ')}
        {...textAreaProps}
      />
      {hint && (
        <p id={hintId} className="mt-1 text-xs text-slate-500">
          {hint}
        </p>
      )}
      {error && (
        <p id={errorId} className="mt-1 text-sm text-red-700">
          {error}
        </p>
      )}
    </div>
  )
}
