import { Link } from '@tanstack/react-router';
import { Alert } from '@components/Alert';
import { Button, buttonClassName } from '@components/Button';
import { Card } from '@components/Card';
import { useAuth } from '@hooks/useAuth';
import { useCreateRoom } from '@hooks/useCreateRoom';
import { vocabService } from '@services/vocab';

const vocab = vocabService.get;

export function HostMatchCard() {
  const user = useAuth();
  const { pending, error, createRoom } = useCreateRoom();

  return (
    <Card title={vocab('host_title')} description={vocab('host_description')}>
      {user ? (
        <Button
          size="lg"
          loading={pending}
          onClick={createRoom}
          className="w-full sm:w-auto"
        >
          {vocab('host_create')}
        </Button>
      ) : (
        <div className="flex flex-col gap-3">
          <p className="text-sm text-muted-foreground">
            {vocab('host_requires_account')}
          </p>
          <div className="flex flex-wrap gap-2">
            <Link to="/login" className={buttonClassName({ size: 'lg' })}>
              {vocab('login')}
            </Link>
            <Link
              to="/register"
              className={buttonClassName({ variant: 'secondary', size: 'lg' })}
            >
              {vocab('register')}
            </Link>
          </div>
        </div>
      )}
      {error && <Alert>{error}</Alert>}
    </Card>
  );
}
