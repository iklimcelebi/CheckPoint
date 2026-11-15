export type ButtonVariant = 'primary' | 'secondary' | 'danger'
export type ButtonSize = 'normal' | 'small'

const VARIANT_CLASSES: Record<ButtonVariant, string> = {
  primary: 'bg-blue-600 text-white hover:bg-blue-700 disabled:bg-blue-300',
  secondary:
    'border border-slate-300 bg-white text-slate-800 hover:bg-slate-100 disabled:text-slate-400',
  danger: 'bg-red-600 text-white hover:bg-red-700 disabled:bg-red-300',
}

const SIZE_CLASSES: Record<ButtonSize, string> = {
  normal: 'px-4 py-2 text-sm',
  small: 'px-3 py-1.5 text-xs',
}

export function buttonClasses(
  variant: ButtonVariant = 'primary',
  size: ButtonSize = 'normal',
  extra = '',
): string {
  return [
    'inline-flex items-center justify-center rounded-md font-medium transition-colors',
    'disabled:cursor-not-allowed',
    VARIANT_CLASSES[variant],
    SIZE_CLASSES[size],
    extra,
  ].join(' ')
}
