
public interface IHealthSystem
{
    void TakeDamage(int damage);
    int GetCurrentHealth();
    int GetMaxHealth();
    bool IsAlive();
}