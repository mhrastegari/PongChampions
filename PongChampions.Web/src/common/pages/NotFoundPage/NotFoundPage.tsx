import { Link } from '@tanstack/react-router';
import { buttonClassName } from '@components/Button';
import { useDocumentTitle } from '@hooks/useDocumentTitle';
import { vocabService } from '@services/vocab';

const vocab = vocabService.get;

interface INotFoundPageProps {
  title?: string;
  description?: string;
}

export function NotFoundPage({ title, description }: INotFoundPageProps) {
  const heading = title ?? vocab('not_found_title');

  useDocumentTitle(heading);

  return (
    <div className="mx-auto flex max-w-md flex-col items-center gap-4 py-12 text-center">
      <p className="font-mono text-6xl font-black text-muted-foreground">404</p>
      <h1 className="text-2xl font-bold">{heading}</h1>
      <p className="text-muted-foreground">
        {description ?? vocab('not_found_description')}
      </p>
      <Link to="/" className={buttonClassName({ size: 'lg' })}>
        {vocab('back_home')}
      </Link>
    </div>
  );
}
