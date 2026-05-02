package com.user.management.repository.jpa;

import com.user.management.enums.Role;
import com.user.management.models.User;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.orm.jpa.DataJpaTest;
import org.springframework.context.annotation.Import;
import org.springframework.test.context.TestPropertySource;

import java.util.List;
import java.util.Optional;

import static org.junit.jupiter.api.Assertions.*;

@DataJpaTest
@Import(JpaUserRepositoryAdapter.class)
@TestPropertySource(properties = {
    "spring.jpa.hibernate.ddl-auto=create-drop",
    "spring.jpa.database-platform=org.hibernate.dialect.H2Dialect"
})
class JpaUserRepositoryAdapterTest {

    @Autowired
    private JpaUserRepositoryAdapter adapter;

    private User buildUser(String login) {
        return new User(null, "Test User", login, "encodedPassword", Role.USER);
    }

    @Test
    void save_shouldPersistAndReturnUserWithGeneratedId() {
        User saved = adapter.save(buildUser("alice"));

        assertNotNull(saved.getId());
        assertEquals("alice", saved.getLogin());
        assertEquals("Test User", saved.getName());
    }

    @Test
    void findAll_shouldReturnAllSavedUsers() {
        adapter.save(buildUser("alice"));
        adapter.save(buildUser("bob"));

        List<User> users = adapter.findAll();

        assertEquals(2, users.size());
    }

    @Test
    void findById_whenExists_shouldReturnUser() {
        User saved = adapter.save(buildUser("alice"));

        Optional<User> found = adapter.findById(saved.getId());

        assertTrue(found.isPresent());
        assertEquals("alice", found.get().getLogin());
    }

    @Test
    void findById_whenNotExists_shouldReturnEmpty() {
        Optional<User> found = adapter.findById("nonexistent-id");

        assertFalse(found.isPresent());
    }

    @Test
    void findByLogin_whenExists_shouldReturnUser() {
        adapter.save(buildUser("alice"));

        Optional<User> found = adapter.findByLogin("alice");

        assertTrue(found.isPresent());
        assertEquals("alice", found.get().getLogin());
    }

    @Test
    void findByLogin_whenNotExists_shouldReturnEmpty() {
        Optional<User> found = adapter.findByLogin("unknown");

        assertFalse(found.isPresent());
    }

    @Test
    void existsByLogin_whenExists_shouldReturnTrue() {
        adapter.save(buildUser("alice"));

        assertTrue(adapter.existsByLogin("alice"));
    }

    @Test
    void existsByLogin_whenNotExists_shouldReturnFalse() {
        assertFalse(adapter.existsByLogin("unknown"));
    }

    @Test
    void existsByLoginAndIdNot_whenLoginBelongsToDifferentUser_shouldReturnTrue() {
        adapter.save(buildUser("alice"));

        assertTrue(adapter.existsByLoginAndIdNot("alice", "different-id"));
    }

    @Test
    void existsByLoginAndIdNot_whenLoginBelongsToSameUser_shouldReturnFalse() {
        User saved = adapter.save(buildUser("alice"));

        assertFalse(adapter.existsByLoginAndIdNot("alice", saved.getId()));
    }

    @Test
    void existsById_whenExists_shouldReturnTrue() {
        User saved = adapter.save(buildUser("alice"));

        assertTrue(adapter.existsById(saved.getId()));
    }

    @Test
    void existsById_whenNotExists_shouldReturnFalse() {
        assertFalse(adapter.existsById("nonexistent-id"));
    }

    @Test
    void deleteById_shouldRemoveUser() {
        User saved = adapter.save(buildUser("alice"));

        adapter.deleteById(saved.getId());

        assertFalse(adapter.existsById(saved.getId()));
    }
}
