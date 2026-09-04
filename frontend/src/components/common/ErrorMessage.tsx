type ErrorMessageProps = {
  message?: string;
  className?: string;
};

export function ErrorMessage({ message, className = "" }: ErrorMessageProps) {
  if (!message) {
    return null;
  }

  return (
    <p className={`error ${className}`.trim()} role="alert">
      {message}
    </p>
  );
}
