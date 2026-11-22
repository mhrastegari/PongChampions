import { Link, useSearch } from '@tanstack/react-router';
import { Alert } from '@components/Alert';
import { Button } from '@components/Button';
import { TextField } from '@components/TextField';
import { useDocumentTitle } from '@hooks/useDocumentTitle';
import { useRedirectWhenSignedIn } from '@hooks/useRedirectWhenSignedIn';
import { vocabService } from '@services/vocab';
import { AuthPanel } from '@widgets/AuthPanel';
import { useLoginForm } from './useLoginForm';

const vocab = vocabService.get;

export function LoginPage() {
  const { redirect } = useSearch({ from: '/login' });
  const form = useLoginForm();

  useDocumentTitle(vocab('login_title'));
  useRedirectWhenSignedIn(redirect ?? '/');

  return (
    <AuthPanel
      title={vocab('login_title')}
      description={vocab('login_description')}
      footer={
        <>
          {vocab('login_no_account')}{' '}
          <Link
            to="/register"
            search={{ redirect }}
            className="font-semibold text-primary underline-offset-4 hover:underline"
          >
            {vocab('register')}
          </Link>
        </>
      }
    >
      <form className="flex flex-col gap-4" onSubmit={form.onSubmit} noValidate>
        <TextField
          label={vocab('field_username')}
          value={form.username}
          onChange={(event) => form.setUsername(event.target.value)}
          autoComplete="username"
          autoCapitalize="none"
          spellCheck={false}
          required
        />
        <TextField
          label={vocab('field_password')}
          type="password"
          value={form.password}
          onChange={(event) => form.setPassword(event.target.value)}
          autoComplete="current-password"
          required
        />
        {form.error && <Alert>{form.error}</Alert>}
        <Button type="submit" size="lg" loading={form.pending}>
          {vocab('login')}
        </Button>
      </form>
    </AuthPanel>
  );
}
