package com.user.management.repository;

import com.user.management.models.User;

import java.util.List;
import java.util.Optional;

/**
 * Database-agnostic contract for user persistence.
 * Active implementation is selected via Spring profile: "mysql" (default) or "mongodb".
 */
public interface UserRepositoryPort {

    User save(User user);

    List<User> findAll();

    Optional<User> findById(String id);

    Optional<User> findByLogin(String login);

    boolean existsByLogin(String login);

    boolean existsByLoginAndIdNot(String login, String id);

    boolean existsById(String id);

    void deleteById(String id);
}
