package com.user.management.repository.mongo;

import com.user.management.models.User;
import com.user.management.models.UserDocument;
import com.user.management.repository.UserRepositoryPort;
import lombok.RequiredArgsConstructor;
import org.springframework.context.annotation.Profile;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.Optional;
import java.util.UUID;

/**
 * MongoDB implementation of {@link UserRepositoryPort}.
 * Active only when the "mongodb" Spring profile is selected.
 *
 * No @Transactional is declared here: single-document operations in MongoDB
 * are atomic by default. Multi-document transactions require a replica set;
 * enable MongoTransactionManager in a separate config if needed.
 */
@Repository
@Profile("mongodb")
@RequiredArgsConstructor
public class MongoUserRepositoryAdapter implements UserRepositoryPort {

    private final UserMongoRepository userMongoRepository;

    @Override
    public User save(User user) {
        if (user.getId() == null || user.getId().isBlank()) {
            user.setId(UUID.randomUUID().toString());
        }
        UserDocument saved = userMongoRepository.save(toDocument(user));
        return toUser(saved);
    }

    @Override
    public List<User> findAll() {
        return userMongoRepository.findAll()
                .stream()
                .map(this::toUser)
                .toList();
    }

    @Override
    public Optional<User> findById(String id) {
        return userMongoRepository.findById(id).map(this::toUser);
    }

    @Override
    public Optional<User> findByLogin(String login) {
        return userMongoRepository.findByLogin(login).map(this::toUser);
    }

    @Override
    public boolean existsByLogin(String login) {
        return userMongoRepository.existsByLogin(login);
    }

    @Override
    public boolean existsByLoginAndIdNot(String login, String id) {
        return userMongoRepository.existsByLoginAndIdNot(login, id);
    }

    @Override
    public boolean existsById(String id) {
        return userMongoRepository.existsById(id);
    }

    @Override
    public void deleteById(String id) {
        userMongoRepository.deleteById(id);
    }

    // ─── Mapping helpers ────────────────────────────────────────────────────

    private UserDocument toDocument(User user) {
        return new UserDocument(
                user.getId(),
                user.getName(),
                user.getLogin(),
                user.getPassword(),
                user.getRole()
        );
    }

    private User toUser(UserDocument doc) {
        return new User(
                doc.getId(),
                doc.getName(),
                doc.getLogin(),
                doc.getPassword(),
                doc.getRole()
        );
    }
}
