import { useId, type ComponentProps } from 'react'

interface SelectFieldProps extends ComponentProps<'select'> {
  label: string
  error?: string
  hint?: string
}

export function SelectField({
  label,
  error,
  hint,
  id,
  className,
  children,
  ...selectProps
}: SelectFieldProps) {
  const generatedId = useId()
  const selectId = id ?? generatedId
  const hintId = hint ? `${selectId}-hint` : undefined
  const errorId = error ? `${selectId}-error` : undefined
  const describedBy = [hintId, errorId].filter(Boolean).join(' ') || undefined

  return (
    <div className={className}>
      <label htmlFor={selectId} className="mb-1 block text-sm font-medium text-slate-800">
        {label}
      </label>
      <select
        id={selectId}
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy}
        className={[
          'block w-full rounded-md border bg-white px-3 py-2 text-sm text-slate-900 shadow-sm',
          'focus:outline-2 focus:outline-offset-0 disabled:bg-slate-100 disabled:text-slate-500',
          error ? 'border-red-500 focus:outline-red-500' : 'border-slate-300 focus:outline-accent',
        ].join(' ')}
        {...selectProps}
      >
        {children}
      </select>
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
