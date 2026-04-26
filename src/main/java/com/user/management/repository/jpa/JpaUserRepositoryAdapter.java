package com.user.management.repository.jpa;

import com.user.management.models.User;
import com.user.management.repository.UserRepositoryPort;
import lombok.RequiredArgsConstructor;
import org.springframework.context.annotation.Profile;
import org.springframework.stereotype.Repository;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;
import java.util.Optional;

/**
 * JPA implementation of {@link UserRepositoryPort}.
 * Active for all profiles except "mongodb" (mysql, test, default).
 * @Transactional is declared here, not on the service layer,
 * so the transaction boundary stays in the persistence adapter.
 */
@Repository
@Profile("!mongodb")
@RequiredArgsConstructor
public class JpaUserRepositoryAdapter implements UserRepositoryPort {

    private final UserJpaRepository userJpaRepository;

    @Override
    @Transactional
    public User save(User user) {
        return userJpaRepository.save(user);
    }

    @Override
    public List<User> findAll() {
        return userJpaRepository.findAll();
    }

    @Override
    public Optional<User> findById(String id) {
        return userJpaRepository.findById(id);
    }

    @Override
    public Optional<User> findByLogin(String login) {
        return userJpaRepository.findByLogin(login);
    }

    @Override
    public boolean existsByLogin(String login) {
        return userJpaRepository.existsByLogin(login);
    }

    @Override
    public boolean existsByLoginAndIdNot(String login, String id) {
        return userJpaRepository.existsByLoginAndIdNot(login, id);
    }

    @Override
    public boolean existsById(String id) {
        return userJpaRepository.existsById(id);
    }

    @Override
    @Transactional
    public void deleteById(String id) {
        userJpaRepository.deleteById(id);
    }
}
