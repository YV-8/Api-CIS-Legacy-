package com.user.management.repository.mongo;

import com.user.management.enums.Role;
import com.user.management.models.User;
import com.user.management.models.UserDocument;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.List;
import java.util.Optional;

import static org.junit.jupiter.api.Assertions.*;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
class MongoUserRepositoryAdapterTest {

    @Mock
    private UserMongoRepository userMongoRepository;

    @InjectMocks
    private MongoUserRepositoryAdapter adapter;

    private UserDocument buildDocument(String id, String login) {
        return new UserDocument(id, "Test User", login, "encodedPassword", Role.USER);
    }

    private User buildUser(String id, String login) {
        return new User(id, "Test User", login, "encodedPassword", Role.USER);
    }

    @Test
    void save_withExistingId_shouldSaveAndReturnMappedUser() {
        User user = buildUser("existing-id", "alice");
        UserDocument saved = buildDocument("existing-id", "alice");
        when(userMongoRepository.save(any(UserDocument.class))).thenReturn(saved);

        User result = adapter.save(user);

        assertEquals("existing-id", result.getId());
        assertEquals("alice", result.getLogin());
        assertEquals("Test User", result.getName());
        verify(userMongoRepository).save(any(UserDocument.class));
    }

    @Test
    void save_withNullId_shouldGenerateIdAndSave() {
        User user = buildUser(null, "alice");
        UserDocument saved = buildDocument("generated-uuid", "alice");
        when(userMongoRepository.save(any(UserDocument.class))).thenReturn(saved);

        User result = adapter.save(user);

        assertNotNull(result.getId());
        verify(userMongoRepository).save(any(UserDocument.class));
    }

    @Test
    void save_withBlankId_shouldGenerateIdAndSave() {
        User user = buildUser("  ", "alice");
        UserDocument saved = buildDocument("generated-uuid", "alice");
        when(userMongoRepository.save(any(UserDocument.class))).thenReturn(saved);

        User result = adapter.save(user);

        assertNotNull(result.getId());
    }

    @Test
    void findAll_shouldReturnMappedUserList() {
        when(userMongoRepository.findAll()).thenReturn(
                List.of(buildDocument("1", "alice"), buildDocument("2", "bob")));

        List<User> result = adapter.findAll();

        assertEquals(2, result.size());
        assertEquals("alice", result.get(0).getLogin());
        assertEquals("bob", result.get(1).getLogin());
    }

    @Test
    void findAll_whenEmpty_shouldReturnEmptyList() {
        when(userMongoRepository.findAll()).thenReturn(List.of());

        List<User> result = adapter.findAll();

        assertTrue(result.isEmpty());
    }

    @Test
    void findById_whenPresent_shouldReturnMappedUser() {
        when(userMongoRepository.findById("1"))
                .thenReturn(Optional.of(buildDocument("1", "alice")));

        Optional<User> result = adapter.findById("1");

        assertTrue(result.isPresent());
        assertEquals("alice", result.get().getLogin());
    }

    @Test
    void findById_whenAbsent_shouldReturnEmpty() {
        when(userMongoRepository.findById("99")).thenReturn(Optional.empty());

        Optional<User> result = adapter.findById("99");

        assertFalse(result.isPresent());
    }

    @Test
    void findByLogin_whenPresent_shouldReturnMappedUser() {
        when(userMongoRepository.findByLogin("alice"))
                .thenReturn(Optional.of(buildDocument("1", "alice")));

        Optional<User> result = adapter.findByLogin("alice");

        assertTrue(result.isPresent());
        assertEquals("alice", result.get().getLogin());
    }

    @Test
    void findByLogin_whenAbsent_shouldReturnEmpty() {
        when(userMongoRepository.findByLogin("unknown")).thenReturn(Optional.empty());

        Optional<User> result = adapter.findByLogin("unknown");

        assertFalse(result.isPresent());
    }

    @Test
    void existsByLogin_shouldDelegateToRepository() {
        when(userMongoRepository.existsByLogin("alice")).thenReturn(true);
        when(userMongoRepository.existsByLogin("unknown")).thenReturn(false);

        assertTrue(adapter.existsByLogin("alice"));
        assertFalse(adapter.existsByLogin("unknown"));
    }

    @Test
    void existsByLoginAndIdNot_shouldDelegateToRepository() {
        when(userMongoRepository.existsByLoginAndIdNot("alice", "other-id")).thenReturn(true);

        assertTrue(adapter.existsByLoginAndIdNot("alice", "other-id"));
    }

    @Test
    void existsById_shouldDelegateToRepository() {
        when(userMongoRepository.existsById("1")).thenReturn(true);
        when(userMongoRepository.existsById("99")).thenReturn(false);

        assertTrue(adapter.existsById("1"));
        assertFalse(adapter.existsById("99"));
    }

    @Test
    void deleteById_shouldDelegateToRepository() {
        doNothing().when(userMongoRepository).deleteById("1");

        assertDoesNotThrow(() -> adapter.deleteById("1"));
        verify(userMongoRepository).deleteById("1");
    }
}
